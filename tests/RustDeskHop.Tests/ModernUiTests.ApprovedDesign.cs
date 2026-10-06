using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Fact]
        public void InlineEditorsKeepTheirTextInsideTheFieldOutline()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(1);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      ClickAddRow(form);
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      DataGridViewRow row = grid.Rows[grid.Rows.Count - 2];
                      foreach (string column in new[] { "ComputerName", "RustDeskId" })
                      {
                          grid.EndEdit();
                          grid.CurrentCell = row.Cells[column];
                          grid.BeginEdit(true);
                          TextBox editor = Assert.IsAssignableFrom<TextBox>(grid.EditingControl);
                          Assert.Equal(grid.CurrentCell.OwningColumn!.HeaderText, editor.AccessibleName);
                          Rectangle cell = grid.GetCellDisplayRectangle(grid.CurrentCell.ColumnIndex, row.Index, false);
                          Rectangle input = grid.RectangleToClient(editor.RectangleToScreen(editor.ClientRectangle));
                          // WinForms clips the text editor to its inset editing panel.
                          input.Intersect(grid.EditingPanel.Bounds);
                          Assert.True(input.Top > cell.Top + (cell.Height - 36) / 2,
                                      $"{column}: input={input}; cell={cell}; panel={grid.EditingPanel.Bounds}");
                          Assert.True(input.Bottom < cell.Top + (cell.Height + 36) / 2,
                                      $"{column}: input={input}; cell={cell}; panel={grid.EditingPanel.Bounds}");
                          editor.Text = column == "ComputerName" ? "New laptop" : "123 456 099";
                      }
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Contains(form.Targets, target => target.Name == "New laptop"
                                                          && target.RustDeskId == "123456099");
                  }
                 );
        }

        [Fact]
        public void GroupedIdentityRetainsTheIdForAssistiveTechnology()
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(3));
                      Load(form);
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      Assert.False(grid.ColumnHeadersVisible);
                      Assert.False(grid.Columns["RustDeskId"]!.Visible);
                      Assert.Contains("RustDesk ID 123 456 001", grid.Rows[0].Cells[0].Value?.ToString());
                      ModernButton manage = Find<ModernButton>(form, "ManageNetworks");
                      Assert.Equal(UiGlyph.SETTINGS, manage.Glyph);
                      Assert.True(manage.Accent);
                  }
                 );
        }

        [Theory]
        [InlineData("123456789", "123 456 789")]
        [InlineData("192.0.2.1", "192.0.2.1")]
        [InlineData("long-custom-id", "long-custom-id")]
        public void DisplayGroupingDoesNotChangeNonNumericAddresses(string id, string expected)
        {
            Assert.Equal(expected, ComputerIdentityPainter.DisplayId(id));
        }

        [Fact]
        public void ManagementFooterDoesNotOverlapAndFitsShortLists()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      Assert.Equal(new Size(810, 396), form.ClientSize);
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      Assert.False(grid.ColumnHeadersVisible);
                      ModernButton save = Find<ModernButton>(form, "SaveNetwork");
                      ModernButton close = Descendants(form).OfType<ModernButton>().Single(b => b.Text == "Close");
                      Assert.False(close.Bounds.IntersectsWith(save.Bounds));
                      Assert.True(close.Right + UiScale.Pixels(form, UiMetrics.GAP) <= save.Left);
                      Assert.Equal(close.Top, save.Top);
                      Assert.All(grid.Rows.Cast<DataGridViewRow>(),
                                 row => Assert.True(row.Height >= UiScale.Pixels(form, 64)));
                  }
                 );
        }

        [Fact]
        public void SectionTabsUseQuietSelectionAndSupportArrowKeys()
        {
            OnSta(() =>
                  {
                      using ProfilesForm form = new ProfilesForm(Settings(1).Profiles);
                      form.Show();
                      Application.DoEvents();
                      SectionTabs tabs = Find<SectionTabs>(form, "NetworkSections");
                      ModernButton computers = Descendants(tabs).OfType<ModernButton>()
                          .Single(b => b.Text == "Computers");
                      ModernButton networks = Descendants(tabs).OfType<ModernButton>()
                          .Single(b => b.Text == "Network settings");
                      Assert.True(computers.Left < networks.Left);
                      Assert.False(computers.Primary);
                      Assert.False(computers.Quiet);
                      Assert.False(networks.Quiet);
                      Assert.True(networks.Accent);
                      Assert.True(computers.Accent);
                      typeof(Control).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .Invoke(computers, [new KeyEventArgs(Keys.Right)]);
                      Application.DoEvents();
                      Assert.Equal(0, tabs.SelectedIndex);
                      Assert.Equal("Network settings", tabs.SelectedTab!.Text);
                      Assert.False(networks.Quiet);
                      Assert.False(computers.Quiet);
                  }
                 );
        }

        [Fact]
        public void WindowSizeStaysFixedWhenChangingNetworksOrAttemptingResize()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      Size fixedSize = form.Size;
                      form.Size = new Size(1920, 1040);
                      Assert.Equal(fixedSize, form.Size);
                      form.Size = new Size(100, 100);
                      Find<ListBox>(form, "Networks").SelectedIndex = 1;
                      Application.DoEvents();
                      Assert.Equal(fixedSize, form.Size);
                      Assert.Equal(form.MinimumSize, form.MaximumSize);
                  }
                 );
        }

        [Fact]
        public void RenderApprovedManagementPreviewWithFictionalData()
        {
            OnSta(() =>
                  {
                      string? output = Environment.GetEnvironmentVariable("RUSTDESKHOP_PREVIEW_DIRECTORY");
                      if (string.IsNullOrWhiteSpace(output))
                          return;

                      AppSettings settings = Settings(2);
                      settings.Profiles[0].Name = "RustDesk Public";
                      settings.Profiles[1].Name = "Studio Private";
                      settings.Targets[1].Name = "Studio workstation";
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets,
                                                                 (_, _, _) => Task.FromResult(ConnectionOutcome.STARTED)
                                                                );
                      form.Show();
                      Application.DoEvents();
                      Find<ListBox>(form, "Networks").SelectedIndex = 1;
                      ClickAddRow(form);
                      Application.DoEvents();
                      foreach (TransitionOverlay overlay in Descendants(form).OfType<TransitionOverlay>().ToArray())
                      {
                          overlay.Finish();
                      }
                      Control content = Assert.Single(form.Controls.Cast<Control>());
                      using Bitmap bitmap = new Bitmap(content.Width, content.Height);
                      content.DrawToBitmap(bitmap, content.ClientRectangle);
                      Directory.CreateDirectory(output);
                      bitmap.Save(Path.Combine(output, "management.png"));
                  }
                 );
        }
        #endregion
    }
}
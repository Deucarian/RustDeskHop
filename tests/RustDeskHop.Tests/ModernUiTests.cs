using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Fact]
        public void RenderDocumentationPreviewWithFictionalData()
        {
            OnSta(() =>
                  {
                      string? output = Environment.GetEnvironmentVariable("RUSTDESKHOP_PREVIEW_DIRECTORY");
                      if (string.IsNullOrWhiteSpace(output))
                          return;

                      Directory.CreateDirectory(output);
                      AppSettings settings = Settings(3);
                      settings.Targets[0].Name = "Home computer";
                      settings.Targets[1].Name = "Studio workstation";
                      settings.Targets[2].Name = "Travel laptop";
                      settings.Profiles[0].Name = "RustDesk Public";
                      settings.Profiles[1].Name = "Example private network";
                      using MainForm form = TestApplication.CreateMainForm(settings);
                      Load(form);
                      form.Show();
                      Application.DoEvents();
                      form.PerformLayout();
                      form.Refresh();
                      Control content = Assert.Single(form.Controls.Cast<Control>());
                      using Bitmap bitmap = new Bitmap(content.Width, content.Height);
                      content.DrawToBitmap(bitmap, content.ClientRectangle);
                      bitmap.Save(Path.Combine(output, "dashboard.png"), System.Drawing.Imaging.ImageFormat.Png);
                      Assert.True(bitmap.Width >= 700);
                  }
                 );
        }

        [Fact]
        public void EmptyListHasClearGuidanceAndDisabledActions()
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(0));
                      Load(form);
                      Assert.True(Find<ModernButton>(form, "ManageNetworks").Enabled);
                      Assert.Empty(Find<ComputerGrid>(form, "Computers").Rows);
                      Assert.Contains("Manage computers & networks.", Find<Label>(form, "NoComputers").Text);
                      Assert.Contains(Descendants(form).OfType<Label>(), l => l.Text.Contains("No computers yet"));
                      Assert.Empty(form.Controls.Find("ApplicationLogo", true));
                      Assert.Empty(form.Controls.Find("TitleBarIcon", true));
                  }
                 );
        }

        [Fact]
        public void EveryComputerHasItsOwnRouteAndConnectButton()
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(3));
                      Load(form);
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      Assert.Equal(3, grid.Rows.Count);
                      grid.CurrentCell = grid.Rows[1].Cells[0];
                      Assert.Equal("Computer 2, RustDesk ID 123 456 002", grid.Rows[1].Cells["Computer"].Value);
                      Assert.Equal("Test private network", grid.Rows[1].Cells["Network"].Value);
                      Assert.All(grid.Rows.Cast<DataGridViewRow>(),
                                 row => Assert.IsType<DataGridViewButtonCell>(row.Cells["Connect"])
                                );
                      Assert.Equal(DataGridViewCellBorderStyle.None, grid.CellBorderStyle);
                      Assert.Equal(AppTheme.selection, grid.DefaultCellStyle.SelectionBackColor);
                      Assert.True(grid.ReadOnly);
                      Assert.False(grid.MultiSelect);
                  }
                 );
        }

        [Theory]
        [InlineData(680, 300)]
        [InlineData(740, 480)]
        [InlineData(916, 529)]
        [InlineData(900, 530)]
        [InlineData(1440, 940)]
        public void MainActionsFitWithoutOverlapping(int width, int height)
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(3));
                      Load(form);
                      form.Size = new Size(width, height);
                      form.PerformLayout();
                      Application.DoEvents();
                      string[] names = ["ManageNetworks", "ComputersCard", "Computers"];
                      Control[] controls = names.Select(n => Assert.Single(form.Controls.Find(n, true))).ToArray();
                      foreach (Control? control in controls)
                      {
                          Assert.True(control.Parent!.ClientRectangle.Contains(control.Bounds),
                                      $"{control.Name}: {control.Bounds}, parent {control.Parent.ClientRectangle}"
                                     );
                      }

                      for (int first = 0; first < controls.Length; first++)
                      {
                          for (int second = first + 1; second < controls.Length; second++)
                          {
                              if (controls[first].Parent == controls[second].Parent)
                              {
                                  Assert.False(controls[first].Bounds.IntersectsWith(controls[second].Bounds),
                                               $"{controls[first].Name} overlaps {controls[second].Name}"
                                              );
                              }
                          }
                      }

                      Assert.All(controls.OfType<ModernButton>(),
                                 b => Assert.True(b.Width >= b.GetPreferredSize(Size.Empty).Width - 2, b.Name)
                                );
                  }
                 );
        }

        [Fact]
        public void RepeatedResizingDoesNotResizeRowsDuringColumnLayout()
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(3));
                      Load(form);
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      for (int iteration = 0; iteration < 4; iteration++)
                      {
                          foreach (Size size in new[]
                                   {
                                       new Size(1920, 1040),
                                       new Size(740, 480),
                                       new Size(900, 530)
                                   })
                          {
                              form.Size = size;
                              form.PerformLayout();
                              Application.DoEvents();
                              int minimum = UiScale.Pixels(form, UiMetrics.COMPUTER_ROW_HEIGHT);
                              Assert.InRange(grid.Rows[0].Height, minimum, minimum + 4);
                              Assert.Equal(3, grid.Rows.Count);
                              Assert.True(Find<SurfacePanel>(form, "ComputersCard").Width <= UiMetrics.CONTENT_WIDTH);
                              Assert.True(grid.Height >= UiMetrics.COMPUTER_ROW_HEIGHT);
                          }
                      }
                  }
                 );
        }

        [Fact]
        public void RefreshingUnchangedComputersKeepsTheFullListVisible()
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(3));
                      Load(form);
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      MethodInfo refresh =
                          typeof(MainForm).GetMethod("RefreshTargets", BindingFlags.Instance | BindingFlags.NonPublic)!;
                      int expectedHeight = grid.ContentHeight;
                      int viewportHeight = grid.Height;
                      Assert.True(viewportHeight >= expectedHeight);
                      for (int attempt = 0; attempt < 3; attempt++)
                      {
                          refresh.Invoke(form, null);
                          Application.DoEvents();
                          Assert.Equal(3, grid.Rows.Count);
                          Assert.Equal(expectedHeight, grid.ContentHeight);
                          Assert.Equal(viewportHeight, grid.Height);
                      }
                  }
                 );
        }

        [Fact]
        public void DashboardHasOnlyRowActionsAndOneSharedFrame()
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(3));
                      Load(form);
                      ModernButton toolbar = Assert.Single(Descendants(form).OfType<ModernButton>());
                      Assert.Equal("ManageNetworks", toolbar.Name);
                      Assert.False(toolbar.Quiet);
                      Assert.False(toolbar.Primary);
                      Assert.Equal(AppTheme.blue, toolbar.ForeColor);
                      SurfacePanel card = Assert.Single(Descendants(form).OfType<SurfacePanel>());
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      Assert.Same(card, grid.Parent);
                      Assert.InRange(grid.Top, 1, 4);
                      Assert.Empty(form.Controls.Find("PageTitle", true));
                      Assert.Equal("Manage computers and networks", toolbar.AccessibleName);
                      Assert.Equal(UiScale.Pixels(form, UiMetrics.FOOTER_GAP), toolbar.Top - card.Bottom);
                      Assert.Equal(UiScale.Pixels(form, UiMetrics.PAGE_INSET),
                                   toolbar.Parent!.ClientSize.Height - toolbar.Bottom
                                  );
                      Assert.InRange(grid.Height - grid.ContentHeight, 0, UiMetrics.INSET);
                      Assert.Equal(UiScale.Pixels(form, UiMetrics.PAGE_INSET), card.Top);
                      Assert.Equal(UiScale.Pixels(form, UiMetrics.PAGE_INSET), card.Left);
                      Assert.Empty(Descendants(form).OfType<Divider>());
                      foreach (string name in new[]
                               {
                                   "PageSubtitle", "SelectedComputer", "SelectedRoute", "CurrentNetwork", "Status",
                                   "AddComputer", "RemoveComputer"
                               })
                          Assert.Empty(form.Controls.Find(name, true));
                  }
                 );
        }

        [Fact]
        public void ManyComputersRemainAvailableAndLongNamesWrap()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(50);
                      settings.Targets[1].Name =
                          "A long computer name that should wrap over several lines instead of disappearing";
                      settings.Profiles[1].Name = "An unusually long private network name that must remain readable";
                      using MainForm form = TestApplication.CreateMainForm(settings);
                      Load(form);
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      Assert.Equal(50, grid.Rows.Count);
                      Assert.True(grid.Rows[1].Height > grid.RowTemplate.MinimumHeight);
                      grid.CurrentCell = grid.Rows[49].Cells[0];
                      Assert.Equal(49, Assert.Single(grid.SelectedRows.Cast<DataGridViewRow>()).Index);
                      Assert.Equal("Computer 50, RustDesk ID 123 456 050", grid.Rows[49].Cells["Computer"].Value);
                  }
                 );
        }

        [Fact]
        public void EditorsUseConsistentButtonsAndContainedFields()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm networks = new ProfilesForm(settings.Profiles);
                      using PublicSignInForm signIn =
                          TestApplication.CreateSignInForm("not-launched-during-this-test.exe");

                      // Construct only: showing the sign-in form would launch RustDesk.
                      foreach (Form? form in new Form[]
                               {
                                   networks,
                                   signIn
                               })
                      {
                          _ = form.Handle;
                          if (form is not PublicSignInForm)
                              form.Show();
                          form.PerformLayout();
                          Application.DoEvents();
                          foreach (InputSurface? field in Descendants(form)
                                       .OfType<InputSurface>()
                                       .Where(field => field.Visible))
                          {
                              Assert.True(field.Parent!.ClientRectangle.Contains(field.Bounds),
                                          $"Field exceeds editor: {field.Bounds}"
                                         );
                              Assert.True(field.Height >= 36);
                          }

                          IEnumerable<Button> buttons = Descendants(form).OfType<Button>();
                          Assert.All(buttons, b => Assert.IsType<ModernButton>(b));
                      }
                  }
                 );
        }

        [Fact]
        public void PublicSignInRetryAndCancelRemainAccessibleAndContained()
        {
            OnSta(() =>
                  {
                      using PublicSignInForm form =
                          TestApplication.CreateSignInForm("not-launched-during-this-test.exe");
                      _ = form.Handle;
                      form.PerformLayout();
                      ModernButton[] buttons = Descendants(form).OfType<ModernButton>().ToArray();
                      Assert.Contains(buttons, b => b.Text == "Retry connection");
                      Assert.Equal("Cancel", Assert.IsType<ModernButton>(form.CancelButton).Text);
                      foreach (ModernButton? button in buttons)
                      {
                          Assert.True(button.TabStop);
                          Assert.True(button.Parent!.ClientRectangle.Contains(button.Bounds),
                                      $"Action is clipped: {button.Text}"
                                     );
                      }

                      Assert.Contains(Descendants(form).OfType<Label>(),
                                      l => l.Text.Contains("RustDesk checks whether the account is accepted.")
                                     );
                  }
                 );
        }

        [Theory]
        [InlineData(780, 490)]
        [InlineData(780, 530)]
        [InlineData(860, 540)]
        [InlineData(1920, 1040)]
        public void NetworkEditorStaysInsideEveryContainer(int width, int height)
        {
            OnSta(() =>
                  {
                      using ProfilesForm form = new ProfilesForm(Settings(3).Profiles);
                      form.Show();
                      form.Size = new Size(width, height);
                      form.PerformLayout();
                      Application.DoEvents();
                      TableLayoutPanel split = Find<TableLayoutPanel>(form, "NetworkPanes");
                      Assert.True(split.Width <= 1080);
                      Assert.True(split.Height <= 500);
                      SectionTabs sections = Find<SectionTabs>(form, "NetworkSections");
                      for (int tab = 0; tab < sections.TabCount; tab++)
                      {
                          sections.SelectedIndex = tab;
                          Application.DoEvents();
                          foreach (Control control in Descendants(form)
                                       .Where(c => c.Visible && c is InputSurface or ModernButton))
                          {
                              for (Control? parent = control.Parent; parent is not null; parent = parent.Parent)
                              {
                                  Rectangle bounds =
                                      parent.RectangleToClient(control.RectangleToScreen(control.ClientRectangle));
                                  Assert.True(parent.ClientRectangle.Contains(bounds),
                                              $"{control.GetType().Name} {control.Text} {bounds} exceeds {parent.GetType().Name} {parent.ClientRectangle}"
                                             );
                              }
                          }
                      }
                  }
                 );
        }

        [Fact]
        public void LongRowLabelsWrapWithoutOverlappingConnectButtons()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      settings.Targets[1].Name = "A long computer name that needs several lines in its computer row";
                      settings.Profiles[1].Name =
                          "A long private network name that remains readable beside the connect button";
                      using MainForm form = TestApplication.CreateMainForm(settings);
                      Load(form);
                      form.Size = form.MinimumSize;
                      form.Show();
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      grid.CurrentCell = grid.Rows[1].Cells[0];
                      Application.DoEvents();
                      grid.FirstDisplayedScrollingRowIndex = 1;
                      Assert.True(grid.Rows[1].Height > UiMetrics.COMPUTER_ROW_HEIGHT);
                      Rectangle route = grid.GetCellDisplayRectangle(2, 1, false);
                      Rectangle action = grid.GetCellDisplayRectangle(3, 1, false);
                      Assert.False(route.IntersectsWith(action));
                      Assert.Equal(UiScale.Pixels(form, 116), action.Width);
                      Assert.True(action.Right <= grid.ClientSize.Width);
                  }
                 );
        }

        [Fact]
        public void ComputerNamesAreEditedOnlyInsideManageNetworks()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using MainForm dashboard = TestApplication.CreateMainForm(settings);
                      Load(dashboard);
                      Assert.Equal(new[]
                                   {
                                       "ManageNetworks"
                                   },
                                   Descendants(dashboard).OfType<ModernButton>().Select(b => b.Name).Order().ToArray()
                                  );
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      SectionTabs sections = Find<SectionTabs>(form, "NetworkSections");
                      sections.SelectedIndex = 1;
                      Assert.Equal("Computers", sections.SelectedTab!.Text);
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      Assert.Equal(3, grid.Rows.Count);
                      Assert.False(grid.Columns["ComputerName"]!.ReadOnly);
                      Assert.True(grid.Rows[0].Cells["RustDeskId"].ReadOnly);
                      EditName(grid, 0, "  Café workstation 🐇  ");
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Equal("Café workstation 🐇", form.Targets[0].Name);
                      Assert.Equal("Computer 1", settings.Targets[0].Name);
                      Assert.Equal(settings.Targets.Select(t => (t.RustDeskId, t.ProfileId)),
                                   form.Targets.Select(t => (t.RustDeskId, t.ProfileId))
                                  );
                      Assert.Equal("Computer 2", form.Targets[1].Name);
                      using TestDirectory temp = new TestDirectory();
                      string path = temp.FilePath("settings.json");
                      ConfigStore.Save(new AppSettings
                                       {
                                           Profiles = form.Profiles,
                                           Targets = form.Targets
                                       },
                                       path
                                      );
                      AppSettings reloaded = ConfigStore.Load(path, null, out string? warning);
                      Assert.Null(warning);
                      Assert.Equal("Café workstation 🐇", reloaded.Targets[0].Name);
                  }
                 );
        }

        [Fact]
        public void SwitchingNetworkDoesNotApplyUnsavedLabelsToOtherComputers()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 1;
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 0, "Unsaved public name");
                      Find<ListBox>(form, "Networks").SelectedIndex = 1;
                      Application.DoEvents();
                      Assert.Equal(2, grid.Rows.Count);
                      Assert.Equal("Computer 2", grid.Rows[0].Cells[0].Value);
                      EditName(grid, 0, "Private workstation");
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Find<ListBox>(form, "Networks").SelectedIndex = 0;
                      Assert.Equal("Computer 1", grid.Rows[0].Cells[0].Value);
                      Assert.Equal("Private workstation", form.Targets[1].Name);
                      Assert.Equal("Computer 1", form.Targets[0].Name);
                  }
                 );
        }

        [Fact]
        public void BlankComputerNameRejectsAllLabelChanges()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 1;
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 0, "Valid but not saved");
                      grid.EndEdit();
                      EditName(grid, 1, "   ");
                      Assert.False(Find<NetworkComputersEditor>(form, "NetworkComputersEditor")
                                       .TrySave(out string? error)
                                  );
                      Assert.Contains("needs a name", error);
                      Assert.Equal(settings.Targets.Select(t => t.Name), form.Targets.Select(t => t.Name));
                  }
                 );
        }

        [Fact]
        public void ClosingWithoutSavingKeepsNamesAndEmptyNetworkExplainsItself()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(1);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 1;
                      EditName(Find<DataGridView>(form, "NetworkComputers"), 0, "Discard me");
                      form.Close();
                      Assert.Equal("Computer 1", form.Targets[0].Name);
                      Assert.Equal("Computer 1", settings.Targets[0].Name);
                      using ProfilesForm emptyForm = new ProfilesForm(settings.Profiles, settings.Targets);
                      emptyForm.Show();
                      Application.DoEvents();
                      Find<SectionTabs>(emptyForm, "NetworkSections").SelectedIndex = 1;
                      Find<ListBox>(emptyForm, "Networks").SelectedIndex = 1;
                      DataGridView emptyGrid = Find<DataGridView>(emptyForm, "NetworkComputers");
                      Assert.True(emptyGrid.Visible);
                      Assert.Single(emptyGrid.Rows.Cast<DataGridViewRow>());
                      Assert.Equal("+ Add computer", emptyGrid.Rows[0].Cells[0].Value);
                      Assert.Null(emptyGrid.Rows[0].Tag);
                  }
                 );
        }

        [Theory]
        [InlineData(780, 530)]
        [InlineData(900, 580)]
        [InlineData(1920, 1040)]
        public void ComputerNameEditorFitsAndSupportsLongLists(int width, int height)
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(60);
                      settings.Targets[0].Name =
                          string.Concat(Enumerable.Repeat("A very long computer name with spaces ", 6));
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      form.Size = new Size(width, height);
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 1;
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      Assert.Equal(31, grid.Rows.Count);
                      grid.CurrentCell = grid.Rows[29].Cells[0];
                      Application.DoEvents();
                      Assert.True(grid.Rows[0].Height > grid.RowTemplate.MinimumHeight,
                                  $"Long-name row: {grid.Rows[0].Height}, minimum: {grid.RowTemplate.MinimumHeight}, value: {grid.Rows[0].Cells[0].Value}"
                                 );
                      Assert.Equal(29, grid.CurrentCell.RowIndex);
                      foreach (Control? control in new Control[]
                               {
                                   grid,
                                   Find<ModernButton>(form, "SaveNetwork")
                               })
                      {
                          for (Control? parent = control.Parent; parent is not null; parent = parent.Parent)
                          {
                              Assert.True(parent.ClientRectangle
                                              .Contains(parent.RectangleToClient(control.RectangleToScreen(control
                                                                        .ClientRectangle
                                                                    )
                                                            )
                                                       )
                                         );
                          }
                      }

                      Assert.True(grid.TabStop);
                  }
                 );
        }
        #endregion

        #region Methods
        private static void EditName(DataGridView grid, int row, string value)
        {
            Application.DoEvents();
            grid.CurrentCell = grid.Rows[row].Cells[0];
            grid.BeginEdit(true);
            Assert.IsAssignableFrom<TextBox>(grid.EditingControl).Text = value;
        }

        private static void Load(MainForm form)
        {
            _ = form.Handle;
            foreach (Control? control in Descendants(form))
            {
                _ = control.Handle;
            }

            typeof(Form).GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form,
                     [EventArgs.Empty]
                );
            form.PerformLayout();
            Application.DoEvents();
        }

        private static T Find<T>(Control root, string name) where T : Control =>
            Assert.IsAssignableFrom<T>(Assert.Single(root.Controls.Find(name, true)));

        private static IEnumerable<Control> Descendants(Control root) =>
            root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));

        private static AppSettings Settings(int count) => new AppSettings()
        {
            Profiles =
            [
                new ServerProfile()
                {
                    Id = "public",
                    Name = "Test public",
                    ServerAddress = "public"
                },
                new ServerProfile()
                {
                    Id = "private",
                    Name = "Test private network",
                    ServerAddress = "example.invalid:21116"
                }
            ],
            Targets = Enumerable
                .Range(1, count)
                .Select(i => new TargetDefinition
                        {
                            Name = $"Computer {i}", RustDeskId = $"123456{i:000}",
                            ProfileId = i % 2 == 0 ? "private" : "public"
                        }
                       )
                .ToList(),
        };

        private static void OnSta(Action action)
        {
            Exception? failure = null;
            Thread thread = new Thread(() =>
                                       {
                                           try
                                           {
                                               Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException,
                                                                                     true);
                                               action();
                                           }
                                           catch (Exception e)
                                           {
                                               failure = e;
                                           }
                                       }
                                      )
            {
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "UI test timed out");
            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
        #endregion
    }
}
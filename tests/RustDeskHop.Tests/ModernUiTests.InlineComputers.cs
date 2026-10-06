using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Fact]
        public void AddCreatesAnEditableRowWithoutOpeningAnotherWindow()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      Find<ListBox>(form, "Networks").SelectedIndex = 1;
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 1;
                      Application.DoEvents();
                      int windowCount = Application.OpenForms.Count;
                      ClickAddRow(form);
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      Assert.Equal(windowCount, Application.OpenForms.Count);
                      Assert.Equal(3, grid.Rows.Count);
                      Assert.Equal(1, grid.CurrentCell!.RowIndex);
                      Assert.True(grid.IsCurrentCellInEditMode);
                      Assert.False(grid.Rows[1].Cells["RustDeskId"].ReadOnly);
                      Assert.True(grid.Rows[0].Cells["RustDeskId"].ReadOnly);
                      ClickAddRow(form);
                      Assert.Equal(3, grid.Rows.Count);
                      EditCell(grid, 1, 0, "  New workstation  ");
                      EditCell(grid, 1, 1, "123 456 999");
                      Assert.Equal(3, form.Targets.Count);
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      TargetDefinition added = form.Targets.Last();
                      Assert.Equal("New workstation", added.Name);
                      Assert.Equal("123456999", added.RustDeskId);
                      Assert.Equal("private", added.ProfileId);
                      Assert.Equal(3, settings.Targets.Count);
                      Assert.True(grid.Rows[1].Cells["RustDeskId"].ReadOnly);
                  }
                 );
        }

        [Fact]
        public void InlineErrorsPreventSavingOrTestingWithoutOpeningAnErrorPopup()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(1);
                      int tests = 0;
                      using ProfilesForm form = new ProfilesForm(settings.Profiles,
                                                                 settings.Targets,
                                                                 (_, _, _) =>
                                                                 {
                                                                     tests++;
                                                                     return Task.FromResult(ConnectionOutcome.STARTED);
                                                                 }
                                                                );
                      form.Show();
                      Application.DoEvents();
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 1;
                      ClickAddRow(form);
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditCell(grid, 1, 0, "Invalid");
                      EditCell(grid, 1, 1, "123@wrong-server");
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Single(form.Targets);
                      Assert.Contains("without a server", Find<Label>(form, "ComputerFeedback").Text);
                      ClickComputerAction(form, 1, "TestComputer");
                      Assert.Equal(0, tests);
                      EditCell(grid, 1, 1, settings.Targets[0].RustDeskId);
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Contains("already saved", Find<Label>(form, "ComputerFeedback").Text);
                      Assert.Single(form.Targets);
                      EditCell(grid, 1, 1, "123456999");
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Equal(2, form.Targets.Count);
                  }
                 );
        }

        [Theory]
        [InlineData((int)ConnectionOutcome.STARTED, "Opened in RustDesk")]
        [InlineData((int)ConnectionOutcome.CANCELLED, "Test cancelled")]
        [InlineData((int)ConnectionOutcome.BLOCKED, "could not start")]
        [InlineData((int)ConnectionOutcome.FAILED, "could not start")]
        public void TestUsesUnsavedInputsWithoutSavingAndRestoresTheEditor(int outcome, string expected)
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(0);
                      TaskCompletionSource<ConnectionOutcome>
                          completion = new TaskCompletionSource<ConnectionOutcome>();
                      int calls = 0;
                      TargetDefinition? tested = null;
                      ServerProfile? route = null;
                      IWin32Window? owner = null;
                      using ProfilesForm form = new ProfilesForm(settings.Profiles,
                                                                 settings.Targets,
                                                                 (window, target, profile) =>
                                                                 {
                                                                     calls++;
                                                                     tested = target;
                                                                     route = profile;
                                                                     owner = window;
                                                                     return completion.Task;
                                                                 }
                                                                );
                      form.Show();
                      Application.DoEvents();
                      Find<ListBox>(form, "Networks").SelectedIndex = 1;
                      Descendants(form).OfType<TextBox>().Single(t => t.AccessibleName == "Server address").Text =
                          "draft.example";
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 1;
                      ClickAddRow(form);
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditCell(grid, 0, 0, "Draft");
                      EditCell(grid, 0, 1, "123 456 999");
                      ClickComputerAction(form, 0, "TestComputer");
                      Assert.True(tested is not null,
                                  $"Feedback: {Find<Label>(form, "ComputerFeedback").Text}; "
                                  + $"name: {grid.Rows[0].Cells[0].Value}; ID: {grid.Rows[0].Cells[1].Value}"
                                 );
                      Assert.Equal("123456999", tested!.RustDeskId);
                      Assert.Equal("private", tested.ProfileId);
                      Assert.Equal("draft.example", route!.ServerAddress);
                      Assert.Same(form, owner);
                      Assert.False(Find<ModernButton>(form, "SaveNetwork").Enabled);
                      Assert.False(Find<ListBox>(form, "Networks").Enabled);
                      Assert.Empty(form.Targets);
                      Assert.Empty(settings.Targets);
                      Assert.Equal("example.invalid:21116", form.Profiles[1].ServerAddress);
                      ClickComputerAction(form, 0, "TestComputer");
                      Assert.Equal(1, calls);
                      form.Close();
                      Assert.False(form.IsDisposed);
                      completion.SetResult((ConnectionOutcome)outcome);
                      Application.DoEvents();
                      Assert.True(Find<ModernButton>(form, "SaveNetwork").Enabled);
                      Assert.True(Find<ListBox>(form, "Networks").Enabled);
                      Assert.Contains(expected, Find<Label>(form, "ComputerFeedback").Text);
                      Assert.Empty(form.Targets);
                  }
                 );
        }

        [Fact]
        public void RemovingAnUnsavedRowOrSwitchingNetworksDoesNotChangeSavedComputers()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(1);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 1;
                      ClickAddRow(form);
                      ClickComputerAction(form, 1, "RemoveComputer");
                      Assert.Single(form.Targets);
                      Assert.Equal(2, Find<DataGridView>(form, "NetworkComputers").Rows.Count);
                      ClickAddRow(form);
                      Find<ListBox>(form, "Networks").SelectedIndex = 1;
                      Find<ListBox>(form, "Networks").SelectedIndex = 0;
                      Assert.Equal(2, Find<DataGridView>(form, "NetworkComputers").Rows.Count);
                      Assert.Single(settings.Targets);
                  }
                 );
        }

        [Theory]
        [InlineData(780, 530)]
        [InlineData(980, 580)]
        public void InlineValidationAndTestControlsRemainInsideTheEditor(int width, int height)
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(0);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      form.Size = new Size(width, height);
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 1;
                      ClickAddRow(form);
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Application.DoEvents();
                      foreach (Control control in Descendants(form)
                                   .Where(c => c.Visible && c is ModernButton or Label or DataGridView))
                      {
                          for (Control? parent = control.Parent; parent is not null; parent = parent.Parent)
                          {
                              Rectangle bounds =
                                  parent.RectangleToClient(control.RectangleToScreen(control.ClientRectangle));
                              Assert.True(parent.ClientRectangle.Contains(bounds), $"{control.Name}: {bounds}");
                          }
                      }
                  }
                 );
        }
        #endregion

        #region Methods
        private static void ClickAddRow(Form form)
        {
            DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
            ClickGridCell(grid, grid.Rows.Count - 1, 0);
        }

        private static void ClickComputerAction(Form form, int rowIndex, string column)
        {
            DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
            ClickGridCell(grid, rowIndex, grid.Columns[column]!.Index);
        }

        private static void ClickGridCell(DataGridView grid, int rowIndex, int columnIndex)
        {
            typeof(DataGridView).GetMethod("OnCellContentClick",
                                          System.Reflection.BindingFlags.Instance
                                          | System.Reflection.BindingFlags.NonPublic
                                         )!
                .Invoke(grid, [new DataGridViewCellEventArgs(columnIndex, rowIndex)]);
            Application.DoEvents();
        }

        private static void EditCell(DataGridView grid, int row, int column, string value)
        {
            Application.DoEvents();
            grid.CurrentCell = grid.Rows[row].Cells[column];
            Assert.True(grid.BeginEdit(true));
            Assert.IsAssignableFrom<TextBox>(grid.EditingControl).Text = value;
            grid.EndEdit();
            Application.DoEvents();
        }
        #endregion
    }
}
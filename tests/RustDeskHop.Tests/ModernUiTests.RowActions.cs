using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Fact]
        public void NetworkDeletionIsOnlyShownInNetworkSettings()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(1);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      ModernButton delete = Find<ModernButton>(form, "DeleteNetwork");
                      Assert.False(delete.Visible);
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 0;
                      Assert.True(delete.Visible);
                      Assert.Equal("Delete network", delete.Text);
                  }
                 );
        }

        [Fact]
        public void RowTestUsesTheClickedComputerRatherThanTheSelectedOne()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      TargetDefinition? tested = null;
                      using ProfilesForm form = new ProfilesForm(settings.Profiles,
                                                                 settings.Targets,
                                                                 (_, target, _) =>
                                                                 {
                                                                     tested = target;
                                                                     return Task.FromResult(ConnectionOutcome.STARTED);
                                                                 }
                                                                );
                      form.Show();
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 0, "Renamed first computer");
                      ClickComputerAction(form, 1, "TestComputer");
                      Assert.Equal("Computer 3", tested!.Name);
                      Assert.Equal(settings.Targets[2].RustDeskId, tested.RustDeskId);
                      Assert.Equal(0, grid.CurrentCell!.RowIndex);
                      Assert.Equal("Computer 1", form.Targets[0].Name);
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Equal("Renamed first computer", form.Targets[0].Name);
                  }
                 );
        }

        [Fact]
        public void RemovingAnotherDraftPreservesTheEditedRow()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(1);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      ClickAddRow(form);
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 0, "Keep this edit");
                      ClickComputerAction(form, 1, "RemoveComputer");
                      Assert.Equal(2, grid.Rows.Count);
                      Assert.Equal("Keep this edit", grid.Rows[0].Cells[0].Value);
                      Assert.Equal("Computer 1", form.Targets[0].Name);
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Equal("Keep this edit", form.Targets[0].Name);
                      Assert.Single(form.Targets);
                  }
                 );
        }

        [Theory]
        [InlineData(Keys.Enter, "ProcessDialogKey")]
        [InlineData(Keys.Space, "OnKeyDown")]
        public void KeyboardActivatesEachRowActionExactlyOnce(Keys key, string method)
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(0);
                      int calls = 0;
                      using ProfilesForm form = new ProfilesForm(settings.Profiles,
                                                                 settings.Targets,
                                                                 (_, _, _) =>
                                                                 {
                                                                     calls++;
                                                                     return Task.FromResult(ConnectionOutcome.STARTED);
                                                                 }
                                                                );
                      form.Show();
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      grid.CurrentCell = grid.Rows[0].Cells[0];
                      PressRowActionKey(grid, key, method);
                      Assert.Equal(2, grid.Rows.Count);
                      Assert.True(grid.IsCurrentCellInEditMode);
                      EditCell(grid, 0, 0, "Keyboard computer");
                      EditCell(grid, 0, 1, "123456999");
                      grid.CurrentCell = grid.Rows[0].Cells["TestComputer"];
                      PressRowActionKey(grid, key, method);
                      Assert.Equal(1, calls);
                      Assert.Empty(form.Targets);
                      grid.CurrentCell = grid.Rows[0].Cells["RemoveComputer"];
                      PressRowActionKey(grid, key, method);
                      Assert.Single(grid.Rows.Cast<DataGridViewRow>());
                      Assert.Null(grid.Rows[0].Tag);
                  }
                 );
        }

        [Fact]
        public void TheAddRowIsNotSavedAsAnEmptyComputer()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(0);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      NetworkComputersEditor editor = Find<NetworkComputersEditor>(form, "NetworkComputersEditor");
                      Assert.True(editor.TrySave(out string? error));
                      Assert.Null(error);
                      Assert.Empty(form.Targets);
                      Assert.False(Find<Label>(form, "ComputerFeedback").Visible);
                  }
                 );
        }
        #endregion

        #region Methods
        private static void PressRowActionKey(DataGridView grid, Keys key, string method)
        {
            object argument = method == "ProcessDialogKey" ? key : new KeyEventArgs(key);
            typeof(RowActionGrid).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(grid, [argument]);
            Application.DoEvents();
        }
        #endregion
    }
}
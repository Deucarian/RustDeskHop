using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Fact]
        public void SavingKeepsRowsAndViewportWhileNormalizingAndLockingNewIds()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(60);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      ClickAddRow(form);
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditCell(grid, 30, 0, "  New laptop  ");
                      EditCell(grid, 30, 1, "123 456 999");
                      grid.CurrentCell = grid.Rows[30].Cells["TestComputer"];
                      int firstRow = grid.FirstDisplayedScrollingRowIndex;
                      DataGridViewRow added = grid.Rows[30];
                      NetworkField(form, "Profile name").Text = "Renamed public network";
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Same(added, grid.Rows[30]);
                      Assert.Equal(30, grid.CurrentCell!.RowIndex);
                      Assert.Equal(firstRow, grid.FirstDisplayedScrollingRowIndex);
                      Assert.Equal("New laptop", added.Cells[0].Value);
                      Assert.Equal("123456999", added.Cells[1].Value);
                      Assert.True(added.Cells[1].ReadOnly);
                      Assert.False(((ComputerDraft)added.Tag!).IsNew);
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Equal(61, form.Targets.Count);
                      ListBox networks = Find<ListBox>(form, "Networks");
                      Assert.Equal("Renamed public network", networks.GetItemText(networks.SelectedItem));
                      networks.SelectedIndex = 1;
                      networks.SelectedIndex = 0;
                      Assert.True(grid.Rows[30].Cells[1].ReadOnly);
                      Assert.Equal("123456999", grid.Rows[30].Cells[1].Value);
                  }
                 );
        }

        [Fact]
        public void AddingANetworkPreservesThePreviousNetworkDraft()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(1);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 0, "Keep while adding a network");
                      NetworkField(form, "Profile name").Text = "Draft public network";
                      Descendants(form).OfType<ModernButton>().Single(button => button.Text == "New network")
                          .PerformClick();
                      NetworkField(form, "Profile name").Text = "Third draft";
                      ListBox networks = Find<ListBox>(form, "Networks");
                      networks.SelectedIndex = 0;
                      Assert.Equal("Draft public network", NetworkField(form, "Profile name").Text);
                      Assert.Equal("Keep while adding a network", grid.Rows[0].Cells[0].Value);
                      networks.SelectedIndex = 2;
                      Assert.Equal("Third draft", NetworkField(form, "Profile name").Text);
                      Assert.Equal("New network", form.Profiles[2].Name);
                      Assert.Equal("Computer 1", form.Targets[0].Name);
                  }
                 );
        }

        [Fact]
        public void ClosingDoesNotSilentlySaveDraftsFromPreviouslyVisitedNetworks()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 0, "Public draft only");
                      NetworkField(form, "Profile name").Text = "Unsaved public network";
                      Find<ListBox>(form, "Networks").SelectedIndex = 1;
                      EditName(grid, 0, "Private draft only");
                      form.Close();
                      Assert.Equal(settings.Targets.Select(target => target.Name),
                                   form.Targets.Select(target => target.Name)
                                  );
                      Assert.Equal(settings.Profiles.Select(profile => profile.Name),
                                   form.Profiles.Select(profile => profile.Name)
                                  );
                  }
                 );
        }
        #endregion
    }
}
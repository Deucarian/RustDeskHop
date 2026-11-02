using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void ReselectingTheActiveNetworkDoesNotRebuildOrResetItsEditor(int page)
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(60);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 12, "Unfinished rename");
                      TextBox editor = Assert.IsAssignableFrom<TextBox>(grid.EditingControl);
                      editor.Select(4, 3);
                      grid.FirstDisplayedScrollingRowIndex = 10;
                      NetworkField(form, "Profile name").Text = "Unfinished network name";
                      Find<SectionTabs>(form, "NetworkSections").SelectedIndex = page;
                      DataGridViewRow row = grid.Rows[12];
                      ListBox networks = Find<ListBox>(form, "Networks");
                      MethodInfo selection = typeof(ListBox)
                          .GetMethod("OnSelectedIndexChanged", BindingFlags.Instance | BindingFlags.NonPublic)!;
                      for (int index = 0; index < 8; index++)
                      {
                          selection.Invoke(networks, [EventArgs.Empty]);
                          Application.DoEvents();
                      }
                      Assert.Same(row, grid.Rows[12]);
                      Assert.Same(editor, grid.EditingControl);
                      Assert.Equal("Unfinished rename", editor.Text);
                      Assert.Equal(4, editor.SelectionStart);
                      Assert.Equal(3, editor.SelectionLength);
                      Assert.Equal(10, grid.FirstDisplayedScrollingRowIndex);
                      Assert.Equal("Unfinished network name", NetworkField(form, "Profile name").Text);
                      Assert.Equal("Computer 25", form.Targets[24].Name);
                      Assert.Equal("Test public", form.Profiles[0].Name);
                  }
                 );
        }

        [Fact]
        public void NetworkFieldsAndComputerDraftsSurviveNavigationWithoutSaving()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      ListBox networks = Find<ListBox>(form, "Networks");
                      EditName(grid, 0, "Public draft");
                      NetworkField(form, "Profile name").Text = "  Draft public name  ";
                      NetworkField(form, "Server address").Text = "";
                      NetworkField(form, "Public key").Text = "fictional-key";
                      NetworkField(form, "Probe host").Text = "probe.example.invalid";
                      Descendants(form).OfType<CheckBox>().Single().Checked = true;
                      Descendants(form).OfType<NumericUpDown>().Single().Value = 22222;
                      networks.SelectedIndex = 1;
                      EditName(grid, 0, "Private saved name");
                      NetworkField(form, "Profile name").Text = "Private saved network";
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      networks.SelectedIndex = 0;
                      Assert.Equal("Public draft", grid.Rows[0].Cells[0].Value);
                      Assert.Equal("  Draft public name  ", NetworkField(form, "Profile name").Text);
                      Assert.Equal("", NetworkField(form, "Server address").Text);
                      Assert.Equal("fictional-key", NetworkField(form, "Public key").Text);
                      Assert.Equal("probe.example.invalid", NetworkField(form, "Probe host").Text);
                      Assert.True(Descendants(form).OfType<CheckBox>().Single().Checked);
                      Assert.Equal(22222, Descendants(form).OfType<NumericUpDown>().Single().Value);
                      Assert.Equal("Test public", form.Profiles[0].Name);
                      Assert.Equal("Computer 1", form.Targets[0].Name);
                      Assert.Equal("Private saved name", form.Targets[1].Name);
                      Assert.Equal("Private saved network", form.Profiles[1].Name);
                      Assert.Equal("Computer 2", settings.Targets[1].Name);
                  }
                 );
        }

        [Fact]
        public void PartiallyEnteredComputerAndValidationFeedbackSurviveNetworkChanges()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(1);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      ClickAddRow(form);
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 1, "Incomplete laptop");
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      string feedback = Find<Label>(form, "ComputerFeedback").Text;
                      Assert.NotEmpty(feedback);
                      ListBox networks = Find<ListBox>(form, "Networks");
                      networks.SelectedIndex = 1;
                      networks.SelectedIndex = 0;
                      Assert.Equal(3, grid.Rows.Count);
                      Assert.Equal("Incomplete laptop", grid.Rows[1].Cells[0].Value);
                      Assert.Equal("", grid.Rows[1].Cells[1].Value);
                      Assert.Equal(feedback, Find<Label>(form, "ComputerFeedback").Text);
                      Assert.False(grid.Rows[1].Cells[1].ReadOnly);
                      Assert.Single(form.Targets);
                  }
                 );
        }

        [Fact]
        public void NetworkNavigationRestoresScrollPositionAndTheCurrentComputer()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(60);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      grid.CurrentCell = grid.Rows[12].Cells["TestComputer"];
                      grid.FirstDisplayedScrollingRowIndex = 10;
                      ListBox networks = Find<ListBox>(form, "Networks");
                      networks.SelectedIndex = 1;
                      grid.CurrentCell = grid.Rows[3].Cells["TestComputer"];
                      grid.FirstDisplayedScrollingRowIndex = 2;
                      networks.SelectedIndex = 0;
                      Assert.Equal(12, grid.CurrentCell!.RowIndex);
                      Assert.Equal("TestComputer", grid.CurrentCell.OwningColumn!.Name);
                      Assert.Equal(10, grid.FirstDisplayedScrollingRowIndex);
                      networks.SelectedIndex = 1;
                      Assert.Equal(3, grid.CurrentCell!.RowIndex);
                      Assert.Equal(2, grid.FirstDisplayedScrollingRowIndex);
                  }
                 );
        }
        #endregion

        #region Methods
        private static TextBox NetworkField(Form form, string label) =>
            Descendants(form).OfType<TextBox>().Single(field => field.AccessibleName == label);
        #endregion
    }
}
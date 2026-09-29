using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Simultria.RustDeskCompanion;
using Xunit;

namespace RustDeskHop.Tests;

public sealed class ModernUiTests
{
    [Fact]
    public void RenderDocumentationPreviewWithFictionalData() => OnSta(() =>
    {
        var output = Environment.GetEnvironmentVariable("RUSTDESKHOP_PREVIEW_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var settings = Settings(3);
        settings.Targets[0].Name = "Home computer";
        settings.Targets[1].Name = "Studio workstation";
        settings.Targets[2].Name = "Travel laptop";
        settings.Profiles[0].Name = "RustDesk Public";
        settings.Profiles[1].Name = "Example private network";
        using var form = new MainForm(settings);
        Load(form);
        form.Show();
        Application.DoEvents();
        Find<Label>(form, "CurrentNetwork").Text = "RustDesk default: Public (example)";
        form.PerformLayout();
        form.Refresh();
        var content = Assert.Single(form.Controls.Cast<Control>());
        using var bitmap = new Bitmap(content.Width, content.Height);
        content.DrawToBitmap(bitmap, content.ClientRectangle);
        bitmap.Save(Path.Combine(output, "dashboard.png"), System.Drawing.Imaging.ImageFormat.Png);
        Assert.True(bitmap.Width >= 700);
    });

    [Fact]
    public void EmptyListHasClearGuidanceAndDisabledActions() => OnSta(() =>
    {
        using var form = new MainForm(Settings(0));
        Load(form);
        Assert.False(Find<ModernButton>(form, "Connect").Enabled);
        Assert.False(Find<ModernButton>(form, "RemoveComputer").Enabled);
        Assert.True(Find<ModernButton>(form, "AddComputer").Enabled);
        Assert.Contains(Descendants(form).OfType<Label>(), l => l.Text.Contains("No computers yet"));
        Assert.Empty(form.Controls.Find("ApplicationLogo", true));
        Assert.Empty(form.Controls.Find("TitleBarIcon", true));
    });

    [Fact]
    public void SelectionUpdatesTheComputerAndAssignedRoute() => OnSta(() =>
    {
        using var form = new MainForm(Settings(3));
        Load(form);
        var grid = Find<ComputerGrid>(form, "Computers");
        Assert.Equal(3, grid.Rows.Count);
        grid.CurrentCell = grid.Rows[1].Cells[0];
        Assert.Contains(Descendants(form).OfType<Label>(), l => l.Text == "Computer 2");
        Assert.Contains(Descendants(form).OfType<Label>(), l => l.Text == "Connect using Test private network");
        Assert.True(Find<ModernButton>(form, "Connect").Enabled);
        Assert.Equal(DataGridViewCellBorderStyle.None, grid.CellBorderStyle);
        Assert.Equal(AppTheme.Selection, grid.DefaultCellStyle.SelectionBackColor);
        Assert.True(grid.ReadOnly);
        Assert.False(grid.MultiSelect);
    });

    [Theory]
    [InlineData(740, 480)]
    [InlineData(916, 529)]
    [InlineData(900, 530)]
    [InlineData(1440, 940)]
    public void MainActionsFitWithoutOverlapping(int width, int height) => OnSta(() =>
    {
        using var form = new MainForm(Settings(3));
        Load(form);
        form.Size = new Size(width, height);
        form.PerformLayout();
        Application.DoEvents();
        var names = new[] { "Connect", "RemoveComputer", "AddComputer", "ManageNetworks", "ComputersCard", "Computers", "PageTitle", "PageSubtitle", "SelectedComputer", "SelectedRoute", "CurrentNetwork", "Status" };
        var controls = names.Select(n => Assert.Single(form.Controls.Find(n, true))).ToArray();
        foreach (var control in controls)
            Assert.True(control.Parent!.ClientRectangle.Contains(control.Bounds), $"{control.Name}: {control.Bounds}, parent {control.Parent.ClientRectangle}");
        for (var first = 0; first < controls.Length; first++)
        for (var second = first + 1; second < controls.Length; second++)
            if (controls[first].Parent == controls[second].Parent)
                Assert.False(controls[first].Bounds.IntersectsWith(controls[second].Bounds), $"{controls[first].Name} overlaps {controls[second].Name}");
        Assert.All(controls.OfType<ModernButton>(), b => Assert.True(b.Width >= b.GetPreferredSize(Size.Empty).Width - 2, b.Name));
    });

    [Fact]
    public void RepeatedResizingDoesNotResizeRowsDuringColumnLayout() => OnSta(() =>
    {
        using var form = new MainForm(Settings(3));
        Load(form);
        var grid = Find<ComputerGrid>(form, "Computers");
        for (var iteration = 0; iteration < 4; iteration++)
        foreach (var size in new[] { new Size(1920, 1040), new Size(740, 480), new Size(900, 530) })
        {
            form.Size = size;
            form.PerformLayout();
            Application.DoEvents();
            Assert.InRange(grid.Rows[0].Height, UiMetrics.RowHeight, UiMetrics.RowHeight + 4);
            Assert.Equal(3, grid.Rows.Count);
            Assert.True(Find<SurfacePanel>(form, "ComputersCard").Width <= UiMetrics.ContentWidth);
            Assert.True(grid.Height <= grid.ContentHeight);
        }
    });

    [Fact]
    public void RefreshingUnchangedComputersKeepsTheFullListVisible() => OnSta(() =>
    {
        using var form = new MainForm(Settings(3));
        Load(form);
        var grid = Find<ComputerGrid>(form, "Computers");
        var refresh = typeof(MainForm).GetMethod("RefreshTargets", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var expectedHeight = grid.ContentHeight;
        Assert.Equal(expectedHeight, grid.Height);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            refresh.Invoke(form, null);
            Application.DoEvents();
            Assert.Equal(3, grid.Rows.Count);
            Assert.Equal(expectedHeight, grid.ContentHeight);
            Assert.Equal(expectedHeight, grid.Height);
        }
    });

    [Fact]
    public void DashboardHasOnePrimaryActionAndOneSharedFrame() => OnSta(() =>
    {
        using var form = new MainForm(Settings(3));
        Load(form);
        var primary = Assert.Single(Descendants(form).OfType<ModernButton>(), b => b.Primary);
        Assert.Equal("Connect", primary.Name);
        Assert.True(Find<ModernButton>(form, "ManageNetworks").Quiet);
        Assert.True(Find<ModernButton>(form, "RemoveComputer").Quiet);
        var card = Assert.Single(Descendants(form).OfType<SurfacePanel>());
        Assert.Same(card, primary.Parent);
        Assert.Same(card, Find<ComputerGrid>(form, "Computers").Parent);
        Assert.Same(card, Find<Label>(form, "SelectedComputer").Parent);
        var grid = Find<ComputerGrid>(form, "Computers");
        Assert.Equal(grid.Left + UiMetrics.CellInset, Find<Label>(form, "SelectedComputer").Left);
        Assert.Equal(grid.Right - UiMetrics.CellInset, primary.Right);
        Assert.Single(Descendants(form).OfType<Divider>());
    });

    [Fact]
    public void ManyComputersRemainAvailableAndLongNamesWrap() => OnSta(() =>
    {
        var settings = Settings(50);
        settings.Targets[1].Name = "A long computer name that should wrap over several lines instead of disappearing";
        settings.Profiles[1].Name = "An unusually long private network name that must remain readable";
        using var form = new MainForm(settings);
        Load(form);
        var grid = Find<ComputerGrid>(form, "Computers");
        Assert.Equal(50, grid.Rows.Count);
        Assert.True(grid.Rows[1].Height > grid.RowTemplate.MinimumHeight);
        grid.CurrentCell = grid.Rows[49].Cells[0];
        Assert.Equal(49, Assert.Single(grid.SelectedRows.Cast<DataGridViewRow>()).Index);
        Assert.Contains(Descendants(form).OfType<Label>(), l => l.Text == "Computer 50");
    });

    [Fact]
    public void EditorsUseConsistentButtonsAndContainedFields() => OnSta(() =>
    {
        var settings = Settings(3);
        using var target = new TargetEditorForm(settings.Profiles, null);
        using var networks = new ProfilesForm(settings.Profiles);
        using var signIn = new PublicSignInForm("not-launched-during-this-test.exe");
        // Construct only: showing the sign-in form would launch RustDesk.
        foreach (var form in new Form[] { target, networks, signIn })
        {
            _ = form.Handle;
            if (form is not PublicSignInForm) form.Show();
            form.PerformLayout();
            Application.DoEvents();
            foreach (var field in Descendants(form).OfType<InputSurface>())
            {
                Assert.True(field.Parent!.ClientRectangle.Contains(field.Bounds), $"Field exceeds editor: {field.Bounds}");
                Assert.True(field.Height >= 36);
            }
            var buttons = Descendants(form).OfType<Button>();
            Assert.All(buttons, b => Assert.IsType<ModernButton>(b));
        }
    });

    [Fact]
    public void PublicSignInRetryAndCancelRemainAccessibleAndContained() => OnSta(() =>
    {
        using var form = new PublicSignInForm("not-launched-during-this-test.exe");
        _ = form.Handle;
        form.PerformLayout();
        var buttons = Descendants(form).OfType<ModernButton>().ToArray();
        Assert.Contains(buttons, b => b.Text == "Retry connection");
        Assert.Equal("Cancel", Assert.IsType<ModernButton>(form.CancelButton).Text);
        foreach (var button in buttons)
        {
            Assert.True(button.TabStop);
            Assert.True(button.Parent!.ClientRectangle.Contains(button.Bounds), $"Action is clipped: {button.Text}");
        }
        Assert.Contains(Descendants(form).OfType<Label>(), l => l.Text.Contains("RustDesk checks whether the account is accepted."));
    });

    [Theory]
    [InlineData(780, 490)]
    [InlineData(780, 530)]
    [InlineData(860, 540)]
    [InlineData(1920, 1040)]
    public void NetworkEditorStaysInsideEveryContainer(int width, int height) => OnSta(() =>
    {
        using var form = new ProfilesForm(Settings(3).Profiles);
        form.Show();
        form.Size = new Size(width, height);
        form.PerformLayout();
        Application.DoEvents();
        var split = Assert.Single(Descendants(form).OfType<SplitContainer>());
        Assert.True(split.Width <= 1080);
        Assert.True(split.Height <= 500);
        foreach (var control in Descendants(form).Where(c => c is InputSurface or ModernButton))
        {
            for (var parent = control.Parent; parent is not null; parent = parent.Parent)
            {
                var bounds = parent.RectangleToClient(control.RectangleToScreen(control.ClientRectangle));
                Assert.True(parent.ClientRectangle.Contains(bounds), $"{control.GetType().Name} {control.Text} {bounds} exceeds {parent.GetType().Name} {parent.ClientRectangle}");
            }
        }
    });

    [Fact]
    public void LongSelectedLabelsWrapWithoutOverlappingActions() => OnSta(() =>
    {
        var settings = Settings(3);
        settings.Targets[1].Name = "A long computer name that needs several lines in the selected computer summary";
        settings.Profiles[1].Name = "A long private network name that should remain fully readable beside the action buttons";
        using var form = new MainForm(settings);
        Load(form);
        form.Size = form.MinimumSize;
        var grid = Find<ComputerGrid>(form, "Computers");
        grid.CurrentCell = grid.Rows[1].Cells[0];
        Application.DoEvents();
        var name = Find<Label>(form, "SelectedComputer");
        var route = Find<Label>(form, "SelectedRoute");
        foreach (var label in new[] { name, route })
        {
            var needed = TextRenderer.MeasureText(label.Text, label.Font, new Size(label.Width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            Assert.True(label.Height >= needed.Height);
            Assert.False(label.Bounds.IntersectsWith(Find<ModernButton>(form, "RemoveComputer").Bounds));
            Assert.False(label.Bounds.IntersectsWith(Find<ModernButton>(form, "Connect").Bounds));
        }
        Assert.True(name.Bottom <= route.Top);
    });

    [Fact]
    public void ComputerNamesAreEditedOnlyInsideManageNetworks() => OnSta(() =>
    {
        var settings = Settings(3);
        using var dashboard = new MainForm(settings);
        Load(dashboard);
        Assert.Equal(new[] { "AddComputer", "Connect", "ManageNetworks", "RemoveComputer" },
            Descendants(dashboard).OfType<ModernButton>().Select(b => b.Name).Order().ToArray());
        using var form = new ProfilesForm(settings.Profiles, settings.Targets);
        form.Show();
        var sections = Find<TabControl>(form, "NetworkSections");
        sections.SelectedIndex = 1;
        Application.DoEvents();
        var grid = Find<DataGridView>(form, "ComputerLabels");
        Assert.Equal(2, grid.Rows.Count);
        Assert.False(grid.Columns["ComputerName"]!.ReadOnly);
        Assert.True(grid.Columns["RustDeskId"]!.ReadOnly);
        EditName(grid, 0, "  Café workstation 🐇  ");
        Find<ModernButton>(form, "SaveNetwork").PerformClick();
        Assert.Equal("Café workstation 🐇", form.Targets[0].Name);
        Assert.Equal("Computer 1", settings.Targets[0].Name);
        Assert.Equal(settings.Targets.Select(t => (t.RustDeskId, t.ProfileId)), form.Targets.Select(t => (t.RustDeskId, t.ProfileId)));
        Assert.Equal("Computer 2", form.Targets[1].Name);
        using var temp = new TestDirectory();
        var path = temp.FilePath("settings.json");
        ConfigStore.Save(new AppSettings { Profiles = form.Profiles, Targets = form.Targets }, path);
        var reloaded = ConfigStore.Load(path, null, out var warning);
        Assert.Null(warning);
        Assert.Equal("Café workstation 🐇", reloaded.Targets[0].Name);
    });

    [Fact]
    public void SwitchingNetworkDoesNotApplyUnsavedLabelsToOtherComputers() => OnSta(() =>
    {
        var settings = Settings(3);
        using var form = new ProfilesForm(settings.Profiles, settings.Targets);
        form.Show();
        Find<TabControl>(form, "NetworkSections").SelectedIndex = 1;
        var grid = Find<DataGridView>(form, "ComputerLabels");
        EditName(grid, 0, "Unsaved public name");
        Find<ListBox>(form, "Networks").SelectedIndex = 1;
        Application.DoEvents();
        Assert.Single(grid.Rows.Cast<DataGridViewRow>());
        Assert.Equal("Computer 2", grid.Rows[0].Cells[0].Value);
        EditName(grid, 0, "Private workstation");
        Find<ModernButton>(form, "SaveNetwork").PerformClick();
        Find<ListBox>(form, "Networks").SelectedIndex = 0;
        Assert.Equal("Computer 1", grid.Rows[0].Cells[0].Value);
        Assert.Equal("Private workstation", form.Targets[1].Name);
        Assert.Equal("Computer 1", form.Targets[0].Name);
    });

    [Fact]
    public void BlankComputerNameRejectsAllLabelChanges() => OnSta(() =>
    {
        var settings = Settings(3);
        using var form = new ProfilesForm(settings.Profiles, settings.Targets);
        form.Show();
        Find<TabControl>(form, "NetworkSections").SelectedIndex = 1;
        var grid = Find<DataGridView>(form, "ComputerLabels");
        EditName(grid, 0, "Valid but not saved");
        grid.EndEdit();
        EditName(grid, 1, "   ");
        Assert.False(Find<ComputerLabelsEditor>(form, "ComputerLabelsEditor").TrySave(out var error));
        Assert.Contains("needs a name", error);
        Assert.Equal(settings.Targets.Select(t => t.Name), form.Targets.Select(t => t.Name));
    });

    [Fact]
    public void ClosingWithoutSavingKeepsNamesAndEmptyNetworkExplainsItself() => OnSta(() =>
    {
        var settings = Settings(1);
        using var form = new ProfilesForm(settings.Profiles, settings.Targets);
        form.Show();
        Find<TabControl>(form, "NetworkSections").SelectedIndex = 1;
        EditName(Find<DataGridView>(form, "ComputerLabels"), 0, "Discard me");
        form.Close();
        Assert.Equal("Computer 1", form.Targets[0].Name);
        Assert.Equal("Computer 1", settings.Targets[0].Name);
        using var emptyForm = new ProfilesForm(settings.Profiles, settings.Targets);
        emptyForm.Show();
        Application.DoEvents();
        Find<TabControl>(emptyForm, "NetworkSections").SelectedIndex = 1;
        Find<ListBox>(emptyForm, "Networks").SelectedIndex = 1;
        Assert.True(Find<Label>(emptyForm, "NoNetworkComputers").Visible);
        Assert.False(Find<DataGridView>(emptyForm, "ComputerLabels").Visible);
    });

    [Theory]
    [InlineData(780, 530)]
    [InlineData(900, 580)]
    [InlineData(1920, 1040)]
    public void ComputerNameEditorFitsAndSupportsLongLists(int width, int height) => OnSta(() =>
    {
        var settings = Settings(60);
        settings.Targets[0].Name = string.Concat(Enumerable.Repeat("A very long computer name with spaces ", 6));
        using var form = new ProfilesForm(settings.Profiles, settings.Targets);
        form.Show();
        form.Size = new Size(width, height);
        Find<TabControl>(form, "NetworkSections").SelectedIndex = 1;
        Application.DoEvents();
        var grid = Find<DataGridView>(form, "ComputerLabels");
        Assert.Equal(30, grid.Rows.Count);
        grid.CurrentCell = grid.Rows[29].Cells[0];
        Application.DoEvents();
        Assert.True(grid.Rows[0].Height > grid.RowTemplate.MinimumHeight, $"Long-name row: {grid.Rows[0].Height}, minimum: {grid.RowTemplate.MinimumHeight}, value: {grid.Rows[0].Cells[0].Value}");
        Assert.Equal(29, grid.CurrentCell.RowIndex);
        foreach (var control in new Control[] { grid, Find<ModernButton>(form, "SaveNetwork") })
        for (var parent = control.Parent; parent is not null; parent = parent.Parent)
            Assert.True(parent.ClientRectangle.Contains(parent.RectangleToClient(control.RectangleToScreen(control.ClientRectangle))));
        Assert.True(grid.TabStop);
    });

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
        foreach (var control in Descendants(form)) _ = control.Handle;
        typeof(Form).GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, [EventArgs.Empty]);
        form.PerformLayout();
        Application.DoEvents();
    }
    private static T Find<T>(Control root, string name) where T : Control => Assert.IsType<T>(Assert.Single(root.Controls.Find(name, true)));
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static AppSettings Settings(int count) => new()
    {
        Profiles = [new() { Id = "public", Name = "Test public", ServerAddress = "public" }, new() { Id = "private", Name = "Test private network", ServerAddress = "example.invalid:21116" }],
        Targets = Enumerable.Range(1, count).Select(i => new TargetDefinition { Name = $"Computer {i}", RustDeskId = $"123456{i:000}", ProfileId = i % 2 == 0 ? "private" : "public" }).ToList(),
    };
    private static void OnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { action(); } catch (Exception e) { failure = e; } }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "UI test timed out");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}

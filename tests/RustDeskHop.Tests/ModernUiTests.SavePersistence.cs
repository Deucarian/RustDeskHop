using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void SavePersistsAndRefreshesWithoutDependingOnHowTheManagerCloses(bool addNetwork, bool checkWhileOpen)
        {
            OnSta(() =>
            {
                string path = Path.Combine(Path.GetTempPath(), $"rustdeskhop-save-{Guid.NewGuid():N}.json");
                try
                {
                    AppSettings settings = Settings(1);
                    ConfigStore.Save(settings, path);
                    RecordingScaleStore store = new RecordingScaleStore(path);
                    ConnectionTestRig rig = new ConnectionTestRig();
                    using MainForm main = new MainForm(store, rig.Workflow, _ => rig.Interaction);
                    main.Show();
                    Application.DoEvents();

                    void AssertSaved()
                    {
                        Assert.Equal(1, store.SaveCount);
                        AppSettings saved = store.Load(out string? warning);
                        Assert.Null(warning);
                        ServerProfile network = Assert.Single(saved.Profiles, p => p.Name == "Saved network");
                        TargetDefinition computer = Assert.Single(saved.Targets, t => t.Name == "Saved computer");
                        Assert.Equal(network.Id, computer.ProfileId);
                        ComputerGrid grid = Find<ComputerGrid>(main, "Computers");
                        ComputerRow row = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ComputerRow>>(grid.DataSource),
                                                        r => r.Name == "Saved computer");
                        Assert.Equal("Saved network", row.ProfileName);
                    }

                    EditManager(main, manager =>
                    {
                        if (addNetwork)
                        {
                            Descendants(manager).OfType<ModernButton>()
                                .Single(button => button.Text == "New network").PerformClick();
                            NetworkField(manager, "Server address").Text = "example.test:21116";
                            Find<SectionTabs>(manager, "NetworkSections").SelectedIndex = 1;
                            ClickAddRow(manager);
                        }
                        NetworkField(manager, "Profile name").Text = "Saved network";
                        DataGridView computers = Find<DataGridView>(manager, "NetworkComputers");
                        if (addNetwork)
                            EditCell(computers, 0, 1, "123456777");
                        EditName(computers, 0, "Saved computer");
                        Find<ModernButton>(manager, "SaveNetwork").PerformClick();
                        if (checkWhileOpen)
                            AssertSaved();
                    });
                    // EditManager closes through Form.Close(), equivalent to the window's X.
                    AssertSaved();
                    EditManager(main, manager =>
                    {
                        Assert.Contains(manager.Profiles, p => p.Name == "Saved network");
                        Assert.Contains(manager.Targets, t => t.Name == "Saved computer");
                    });
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void FailedSaveKeepsEditsAndNewIdsEditableUntilRetrySucceeds()
        {
            OnSta(() =>
            {
                AppSettings settings = Settings(1);
                AppSettings? persisted = null;
                bool allowSave = false;
                using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets,
                    saveSettings: (_, candidate) =>
                    {
                        if (!allowSave)
                            return false;
                        persisted = candidate;
                        return true;
                    });
                form.Show();
                Application.DoEvents();
                NetworkField(form, "Profile name").Text = "Saved name";
                ClickAddRow(form);
                DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                EditCell(grid, 1, 0, "New computer");
                EditCell(grid, 1, 1, "123 456 777");
                DataGridViewRow row = grid.Rows[1];
                Find<ModernButton>(form, "SaveNetwork").PerformClick();
                Assert.Null(persisted);
                Assert.Single(form.Targets);
                Assert.Equal(settings.Profiles[0].Name, form.Profiles[0].Name);
                Assert.Equal("Saved name", NetworkField(form, "Profile name").Text);
                Assert.Same(row, grid.Rows[1]);
                Assert.Equal("New computer", row.Cells[0].Value);
                Assert.False(row.Cells[1].ReadOnly);
                Assert.True(((ComputerDraft)row.Tag!).IsNew);

                allowSave = true;
                EditCell(grid, 1, 1, "123 456 778");
                Find<ModernButton>(form, "SaveNetwork").PerformClick();
                Assert.NotNull(persisted);
                Assert.Equal("Saved name", persisted.Profiles[0].Name);
                Assert.Equal("123456778", persisted.Targets[1].RustDeskId);
                Assert.True(row.Cells[1].ReadOnly);
                Assert.Same(row, grid.Rows[1]);
                // Neither continued editing nor the manager's saved objects can mutate the owner snapshot.
                form.Profiles[0].Name = "Later edit";
                form.Targets[1].Name = "Later computer edit";
                Assert.Equal("Saved name", persisted.Profiles[0].Name);
                Assert.Equal("New computer", persisted.Targets[1].Name);
                Assert.Single(settings.Targets);
            });
        }

        [Fact]
        public void SaveExcludesOtherNetworkDraftsAndIncompleteNewNetworks()
        {
            OnSta(() =>
            {
                AppSettings settings = Settings(3);
                AppSettings? persisted = null;
                using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets,
                    saveSettings: (_, candidate) =>
                    {
                        ConfigStore.Validate(candidate);
                        persisted = candidate;
                        return true;
                    });
                form.Show();
                Application.DoEvents();
                NetworkField(form, "Profile name").Text = "Unfinished public network";
                DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                EditName(grid, 0, "Unfinished public computer");
                Descendants(form).OfType<ModernButton>()
                    .Single(button => button.Text == "New network").PerformClick();
                ListBox networks = Find<ListBox>(form, "Networks");
                networks.SelectedIndex = 1;
                NetworkField(form, "Profile name").Text = "Saved private network";
                Find<SectionTabs>(form, "NetworkSections").SelectedIndex = 1;
                EditName(grid, 0, "Saved private computer");
                Find<ModernButton>(form, "SaveNetwork").PerformClick();
                Assert.NotNull(persisted);
                Assert.Equal(2, persisted.Profiles.Count);
                Assert.Equal(settings.Profiles[0].Name, persisted.Profiles[0].Name);
                Assert.Equal("Saved private network", persisted.Profiles[1].Name);
                Assert.Equal(["Computer 1", "Saved private computer", "Computer 3"],
                             persisted.Targets.Select(t => t.Name));
                Assert.Equal(3, form.Profiles.Count);
                networks.SelectedIndex = 0;
                Assert.Equal("Unfinished public network", NetworkField(form, "Profile name").Text);
                Assert.Equal("Unfinished public computer", grid.Rows[0].Cells[0].Value);
            });
        }

        [Fact]
        public void CloseAfterSaveDoesNotCommitLaterEditsOrAnIncompleteNewNetwork()
        {
            OnSta(() =>
            {
                using TestDirectory temporary = new TestDirectory();
                string path = temporary.FilePath("settings.json");
                AppSettings settings = Settings(1);
                ConfigStore.Save(settings, path);
                RecordingScaleStore store = new RecordingScaleStore(path);
                ConnectionTestRig rig = new ConnectionTestRig();
                using MainForm main = new MainForm(store, rig.Workflow, _ => rig.Interaction);
                main.Show();
                Application.DoEvents();
                EditManager(main, manager =>
                {
                    DataGridView grid = Find<DataGridView>(manager, "NetworkComputers");
                    EditName(grid, 0, "Saved computer");
                    Find<ModernButton>(manager, "SaveNetwork").PerformClick();
                    EditName(grid, 0, "Unfinished later edit");
                    Descendants(manager).OfType<ModernButton>()
                        .Single(button => button.Text == "New network").PerformClick();
                    Descendants(manager).OfType<ModernButton>()
                        .Single(button => button.Text == "Close").PerformClick();
                });
                Assert.Equal(1, store.SaveCount);
                AppSettings saved = store.Load(out string? warning);
                Assert.Null(warning);
                Assert.Equal(2, saved.Profiles.Count);
                Assert.Equal("Saved computer", Assert.Single(saved.Targets).Name);
                EditManager(main, manager => Assert.Equal("Saved computer", Assert.Single(manager.Targets).Name));
            });
        }

        private static void EditManager(MainForm main, Action<ProfilesForm> edit)
        {
            Exception? failure = null;
            using System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 15 };
            timer.Tick += (_, _) =>
            {
                ProfilesForm? manager = main.OwnedForms.OfType<ProfilesForm>().FirstOrDefault();
                if (manager is null || !manager.Visible || Find<ListBox>(manager, "Networks").Items.Count == 0)
                    return;
                timer.Stop();
                try
                {
                    edit(manager);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    manager.Close();
                }
            };
            timer.Start();
            Find<ModernButton>(main, "ManageNetworks").PerformClick();
            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}

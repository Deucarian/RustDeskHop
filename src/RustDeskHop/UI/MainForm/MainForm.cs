using RustDeskHop.Models;
using RustDeskHop.Connections;
using RustDeskHop.Settings;
using RustDeskHop.UI.Controls;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI
{
    internal sealed partial class MainForm : BrandedForm
    {
        #region Constants and Fields
        private AppSettings _settings = null!;
        private string? _settingsWarning;
        private bool _connecting;
        private readonly ISettingsStore _settingsStore;
        private readonly IConnectionWorkflow _connections;
        private readonly IConnectionInteraction _connectionInteraction;
        private readonly Func<IWin32Window, IConnectionInteraction> _createInteraction;
        private readonly ContentTransition _listTransition;
        #endregion

        #region Constructors and Destructors
        internal MainForm(ISettingsStore settingsStore,
                          IConnectionWorkflow connections,
                          Func<IWin32Window, IConnectionInteraction> createInteraction,
                          AppSettings? initialSettings = null)
        {
            _settingsStore = settingsStore;
            _connections = connections;
            _createInteraction = createInteraction;
            _connectionInteraction = createInteraction(this);
            _settings = initialSettings!;
            Text = "RustDeskHop";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(740, 320);
            ClientSize = new Size(900, 300);
            BuildUi();
            _listTransition = new ContentTransition(_targetsGrid);
            Load += (_, _) =>
            {
                _settings ??= settingsStore.Load(out _settingsWarning);
                RefreshTargets();
            };
            Shown += (_, _) =>
            {
                if (_settingsWarning is not null)
                {
                    MessageBox.Show(this,
                                    _settingsWarning,
                                    "Saved settings need attention",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                   );
                }
            };
        }
        #endregion

        #region Methods
        private void RefreshTargets()
        {
            _listTransition.Begin();
            _targetsGrid.DataSource = _settings
                .Targets.Select(target => new ComputerRow(target.Name,
                                                          target.RustDeskId,
                                                          _settings.Profiles
                                                              .FirstOrDefault(p => p.Id == target.ProfileId)
                                                              ?.Name
                                                          ?? "Missing network"
                                                         )
                               )
                .ToList();
            foreach (DataGridViewRow row in _targetsGrid.Rows)
            {
                TargetDefinition target = _settings.Targets[row.Index];
                row.Tag = (target.ProfileId, target.RustDeskId);
            }
            _emptyState.Visible = _settings.Targets.Count == 0;
            _targetsGrid.Visible = _settings.Targets.Count > 0;
            WindowContent.PerformLayout();
            _listTransition.End(true);
        }

        private async Task ConnectRowAsync(int index)
        {
            if (_connecting || index < 0 || index >= _settings.Targets.Count)
                return;

            _connecting = true;
            _targetsGrid.SetOpeningRow(index);
            _profilesButton.Enabled = false;
            try
            {
                await _connections.ConnectAsync(_settings.Targets[index], _settings, _connectionInteraction);
            }
            finally
            {
                _connecting = false;
                _targetsGrid.SetOpeningRow(null);
                _profilesButton.Enabled = true;
            }
        }

        private void ManageProfiles()
        {
            using ProfilesForm dialog = new ProfilesForm(_settings.Profiles, _settings.Targets, TestComputerAsync);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                SaveSettings(new AppSettings
                             {
                                 Profiles = dialog.Profiles,
                                 Targets = dialog.Targets
                             }
                            );
            }
        }

        private Task<ConnectionOutcome> TestComputerAsync(IWin32Window owner,
                                                          TargetDefinition target,
                                                          ServerProfile profile)
        {
            // Test an isolated snapshot, including unsaved inputs, without saving it.
            AppSettings preview = new AppSettings
            {
                Profiles = [.._settings.Profiles.Where(p => p.Id != profile.Id), profile],
                Targets = [target]
            };
            return _connections.ConnectAsync(target, preview, _createInteraction(owner));
        }

        private void SaveSettings(AppSettings updated)
        {
            try
            {
                _settingsStore.Save(updated);
                _settings = updated;
                RefreshTargets();
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                MessageBox.Show(this,
                                $"Your changes could not be saved. The previous settings were kept.\n\n{ex.Message}",
                                "Settings not saved",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                               );
            }
        }
        #endregion
    }
}
using RustDeskHop.Models;
using RustDeskHop.UI.Theme;
using RustDeskHop.UI.Controls;
using RustDeskHop.Connections;

namespace RustDeskHop.UI
{
    internal sealed partial class NetworkComputersEditor : UserControl, IUiScaleAware
    {
        #region Constants and Fields
        private readonly RowActionGrid _grid = new RowActionGrid();
        private NetworkComputerDrafts _drafts = new NetworkComputerDrafts("", []);
        private List<TargetDefinition> _targets = [];
        private ServerProfile? _profile;
        private readonly Func<TargetDefinition, Task<ConnectionOutcome?>>? _testConnection;
        private bool _testing;
        private bool _rebuilding;
        private int? _testingRow;
        private readonly Label _feedback = AppTheme.Label("", AppTheme.small, AppTheme.muted);
        private readonly ContentTransition _listTransition;
        #endregion

        #region Constructors and Destructors
        internal NetworkComputersEditor(Func<TargetDefinition, Task<ConnectionOutcome?>>? testConnection = null)
        {
            _testConnection = testConnection;
            AutoScaleMode = AutoScaleMode.None;
            BuildUi();
            _listTransition = new ContentTransition(_grid);
        }
        #endregion

        #region Delegates and Events
        internal event EventHandler? ContentHeightChanged;
        #endregion

        #region Properties and Indexers
        internal int ContentHeight => _grid.Rows.Cast<DataGridViewRow>().Sum(row => row.Height)
            + (_feedback.Visible ? _feedback.Height + _feedback.Margin.Top : 0);
        #endregion

        #region Methods
        internal void LoadNetwork(ServerProfile? profile, List<TargetDefinition> targets)
        {
            // Switching networks discards unsaved edits, just like the network fields.
            _grid.CancelEdit();
            _profile = profile;
            _targets = targets;
            _drafts = new NetworkComputerDrafts(profile?.Id ?? "", targets);
            ShowFeedback("");
            RebuildRows(false);
        }

        internal bool TrySave(out string? error)
        {
            EndEdit();
            if (!ValidateRows(true))
            {
                error = _feedback.Text;
                return false;
            }
            bool saved = _drafts.TrySave(_targets, out error);
            ShowFeedback(error ?? "");
            return saved;
        }

        private void AddComputer()
        {
            if (_profile is null || _testing)
                return;

            EndEdit();
            int incomplete = _drafts
                .Items.ToList()
                .FindIndex(d => string.IsNullOrWhiteSpace(d.Name) || string.IsNullOrWhiteSpace(d.RustDeskId));
            if (incomplete >= 0)
            {
                _grid.CurrentCell =
                    _grid
                        .Rows[incomplete]
                        .Cells[string.IsNullOrWhiteSpace(_drafts.Items[incomplete].Name)
                                   ? "ComputerName"
                                   : "RustDeskId"];
            }
            else
            {
                ComputerDraft added = _drafts.AddBlank();
                RebuildRows();
                _grid.CurrentCell = _grid.Rows[_drafts.Items.IndexOf(added)].Cells[0];
            }
            ShowFeedback("");
            _grid.Focus();
            _grid.BeginEdit(true);
        }

        internal void ShowFeedback(string message)
        {
            _feedback.Text = message;
            _feedback.Visible = message.Length > 0;
            ContentHeightChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task ActivateCellAsync(int rowIndex, int columnIndex)
        {
            if (_testing || rowIndex < 0 || columnIndex < 0
                || _grid.Rows[rowIndex].Cells[columnIndex] is not RowActionCell { ActionEnabled: true })
                return;

            DataGridViewRow row = _grid.Rows[rowIndex];
            if (row.Tag is not ComputerDraft)
                AddComputer();
            else if (_grid.Columns[columnIndex].Name == "TestComputer")
                await TestRowAsync(rowIndex);
            else if (_grid.Columns[columnIndex].Name == "RemoveComputer")
                RemoveComputer(rowIndex);
        }

        private async Task TestRowAsync(int rowIndex)
        {
            if (_testing || _testConnection is null || _grid.Rows[rowIndex].Tag is not ComputerDraft draft)
                return;

            EndEdit();
            string? error = _drafts.Validate(draft);
            if (error is not null)
            {
                ShowFeedback(error);
                ValidateRows(true);
                return;
            }

            _testing = true;
            _testingRow = rowIndex;
            _grid.Enabled = false;
            UpdateActions();
            ShowFeedback("Opening a test in RustDesk. This does not save the computer.");
            try
            {
                ConnectionOutcome? outcome = await _testConnection(_drafts.Snapshot(draft));
                if (outcome is not null)
                {
                    ShowFeedback(outcome switch
                                 {
                                     ConnectionOutcome.STARTED =>
                                         "Opened in RustDesk. Check the remote screen there; connection success isn't verified here.",
                                     ConnectionOutcome.CANCELLED => "Test cancelled. No computer changes were saved.",
                                     _ => "Test could not start. Resolve the reported problem, then try again."
                                 }
                                );
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Test could not start: {ex.Message}");
            }
            finally
            {
                _testing = false;
                _testingRow = null;
                _grid.Enabled = true;
                UpdateActions();
            }
        }

        private void RemoveComputer(int rowIndex)
        {
            if (_testing || _grid.Rows[rowIndex].Tag is not ComputerDraft draft)
                return;

            EndEdit();
            if (draft.IsNew)
            {
                _grid.CancelEdit();
                _drafts.Items.Remove(draft);
                ShowFeedback("");
                RebuildRows();
                return;
            }
            if (MessageBox.Show(this,
                                $"Remove “{draft.Name}” from this network?\n\nThis only removes the saved entry in RustDeskHop.",
                                "Remove computer",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question
                               )
                != DialogResult.Yes)
                return;

            _grid.CancelEdit();
            _drafts.Items.Remove(draft);
            RebuildRows();
        }
        #endregion
    }
}
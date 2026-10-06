using RustDeskHop.UI.Controls;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI
{
    internal sealed partial class NetworkComputersEditor
    {
        #region Methods
        public void ApplyUiScale()
        {
            int Px(int value) => UiScale.Pixels(this, value);
            _grid.DefaultCellStyle.Font = UiScale.FontFor(this, AppTheme.body);
            _grid.RowTemplate.MinimumHeight = Px(UiMetrics.COMPUTER_ROW_HEIGHT);
            _grid.Columns["TestComputer"]!.Width = Px(92);
            _grid.Columns["RemoveComputer"]!.Width = Px(76);
            foreach (DataGridViewRow row in _grid.Rows)
            {
                row.MinimumHeight = _grid.RowTemplate.MinimumHeight;
                if (row.Tag is not ComputerDraft draft)
                    continue;

                row.Cells["ComputerName"].Style.Font = UiScale.FontFor(this,
                                                                    draft.IsNew ? AppTheme.body : AppTheme.strong
                                                                   );
                row.Cells["ComputerName"].Style.Padding = draft.IsNew
                    ? new Padding(Px(UiMetrics.IDENTITY_INSET + 8), Px(18), Px(16), Px(18))
                    : new Padding(Px(UiMetrics.IDENTITY_INSET), Px(8), Px(8), Px(28));
                row.Cells["RustDeskId"].Style.Padding = new Padding(Px(12), Px(18), Px(12), Px(18));
            }
            _grid.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
            if (_grid.EditingControl is Control editor && _grid.CurrentCell is DataGridViewCell current)
                editor.Font = current.InheritedStyle.Font;
            ContentHeightChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateActions()
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                foreach (RowActionCell cell in row.Cells.OfType<RowActionCell>())
                {
                    cell.ActionEnabled = !_testing
                        && _profile is not null
                        && (cell.OwningColumn?.Name != "TestComputer" || _testConnection is not null);
                }
                if (row.Tag is ComputerDraft)
                    row.Cells["TestComputer"].Value = _testingRow == row.Index ? "Opening…" : "Test";
            }
            _grid.Invalidate();
        }

        private void EndEdit()
        {
            _grid.EndEdit();
            // Read final cell values at the action boundary, including edits that were still in progress.
            foreach (DataGridViewRow row in _grid.Rows)
            {
                SynchronizeRow(row.Index);
            }
        }

        private void RebuildRows(bool animate = true)
        {
            if (animate)
                _listTransition.Begin();
            else
                _listTransition.Cancel();
            _rebuilding = true;
            try
            {
                _grid.Rows.Clear();
                foreach (ComputerDraft draft in _drafts.Items)
                {
                    int index = _grid.Rows.Add(draft.Name, draft.RustDeskId, "Test", "Remove");
                    DataGridViewRow row = _grid.Rows[index];
                    row.Tag = draft;
                    row.Cells["RustDeskId"].ReadOnly = !draft.IsNew;
                    row.Cells["TestComputer"].ToolTipText = $"Test connection to {draft.Name}";
                    row.Cells["RemoveComputer"].ToolTipText = $"Remove {draft.Name} from this network";
                }
                DataGridViewRow addRow = _grid.Rows[_grid.Rows.Add()];
                addRow.ReadOnly = true;
                addRow.Cells[0] = new RowActionCell
                {
                    Value = "+ Add computer", ToolTipText = "Add a computer here", AddAction = true, Quiet = true
                };
                for (int column = 1; column < _grid.ColumnCount; column++)
                {
                    addRow.Cells[column] = new DataGridViewTextBoxCell { Value = "" };
                }
                ApplyUiScale();
                UpdateActions();
            }
            finally
            {
                _rebuilding = false;
            }
            ContentHeightChanged?.Invoke(this, EventArgs.Empty);
            if (animate)
                _listTransition.End(true);
        }

        private void CommitCell(int rowIndex)
        {
            if (_rebuilding)
                return;

            SynchronizeRow(rowIndex);
            ValidateRows(false);
            ShowFeedback("");
        }

        private void SynchronizeRow(int rowIndex)
        {
            if (_rebuilding || rowIndex < 0 || _grid.Rows[rowIndex].Tag is not ComputerDraft draft)
                return;

            DataGridViewRow row = _grid.Rows[rowIndex];
            draft.Name = row.Cells["ComputerName"].Value?.ToString() ?? "";
            if (draft.IsNew)
                draft.RustDeskId = row.Cells["RustDeskId"].Value?.ToString() ?? "";
            row.Cells["TestComputer"].ToolTipText = $"Test connection to {draft.Name}";
            row.Cells["RemoveComputer"].ToolTipText = $"Remove {draft.Name} from this network";
        }

        private bool ValidateRows(bool focusError)
        {
            DataGridViewRow? firstError = null;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Tag is not ComputerDraft draft)
                    continue;

                row.ErrorText = _drafts.Validate(draft) ?? "";
                if (row.ErrorText.Length > 0)
                    firstError ??= row;
            }
            if (focusError && firstError is not null)
            {
                ShowFeedback(firstError.ErrorText);
                _grid.CurrentCell =
                    firstError.Cells[string.IsNullOrWhiteSpace(((ComputerDraft)firstError.Tag!).Name)
                                         ? "ComputerName"
                                         : "RustDeskId"];
                _grid.Focus();
            }
            return firstError is null;
        }
        #endregion
    }
}
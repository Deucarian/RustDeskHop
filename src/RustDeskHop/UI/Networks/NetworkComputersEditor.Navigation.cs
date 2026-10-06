namespace RustDeskHop.UI
{
    internal sealed partial class NetworkComputersEditor
    {
        #region Methods
        private void CaptureDraftView()
        {
            if (_activeDraft is null)
                return;

            EndEdit();
            _activeDraft.CurrentRow = _grid.CurrentCell?.RowIndex ?? -1;
            _activeDraft.CurrentColumn = _grid.CurrentCell?.ColumnIndex ?? 0;
            _activeDraft.FirstVisibleRow = Math.Max(0, _grid.FirstDisplayedScrollingRowIndex);
            _activeDraft.HorizontalOffset = _grid.HorizontalScrollingOffset;
            _activeDraft.Feedback = _feedback.Text;
        }

        private void RestoreDraftView()
        {
            DataGridViewEditMode editMode = _grid.EditMode;
            _grid.EditMode = DataGridViewEditMode.EditProgrammatically;
            try
            {
                if (_activeDraft is not null && _grid.Rows.Count > 0)
                {
                    if (_activeDraft.CurrentRow >= 0)
                    {
                        _grid.CurrentCell = _grid.Rows[Math.Min(_activeDraft.CurrentRow, _grid.Rows.Count - 1)]
                            .Cells[Math.Min(_activeDraft.CurrentColumn, _grid.ColumnCount - 1)];
                    }
                    int firstVisibleRow = Math.Min(_activeDraft.FirstVisibleRow, _grid.Rows.Count - 1);
                    _grid.FirstDisplayedScrollingRowIndex = firstVisibleRow;
                    _grid.HorizontalScrollingOffset = _activeDraft.HorizontalOffset;
                }
            }
            finally
            {
                _grid.EditMode = editMode;
            }
            ShowFeedback(_activeDraft?.Feedback ?? "");
        }

        private void RefreshSavedRows()
        {
            _rebuilding = true;
            try
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.Tag is not ComputerDraft draft)
                        continue;

                    row.Cells["ComputerName"].Value = draft.Name;
                    row.Cells["RustDeskId"].Value = draft.RustDeskId;
                    row.Cells["RustDeskId"].ReadOnly = !draft.IsNew;
                }
                ApplyUiScale();
            }
            finally
            {
                _rebuilding = false;
            }
        }
        #endregion
    }
}
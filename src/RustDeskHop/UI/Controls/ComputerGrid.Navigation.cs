namespace RustDeskHop.UI.Controls
{
    internal sealed partial class ComputerGrid
    {
        #region Constants and Fields
        private bool _keyboardNavigation;
        #endregion

        #region Methods
        protected override void SetSelectedCellCore(int columnIndex, int rowIndex, bool selected) =>
            base.SetSelectedCellCore(columnIndex, rowIndex, false);

        protected override void SetSelectedRowCore(int rowIndex, bool selected) =>
            base.SetSelectedRowCore(rowIndex, false);

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            _keyboardNavigation = ShowFocusCues;
            FocusConnect((ModifierKeys & Keys.Shift) != 0 ? Rows.Count - 1 : 0);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _keyboardNavigation = false;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            _keyboardNavigation = true;
            if (e.Modifiers == Keys.None && e.KeyCode is Keys.Enter or Keys.Space)
            {
                e.SuppressKeyPress = true;
                ActivateFocusedConnect();
                return;
            }
            base.OnKeyDown(e);
            if (e.Modifiers == Keys.None
                && e.KeyCode is Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown)
                FocusConnect(CurrentCell?.RowIndex ?? 0);
            Invalidate();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            _keyboardNavigation = true;
            if (keyData is Keys.Enter or Keys.Space)
            {
                ActivateFocusedConnect();
                return true;
            }
            if (keyData is Keys.Tab or (Keys.Shift | Keys.Tab))
                return MoveBetweenConnectButtons(keyData == Keys.Tab);
            return base.ProcessDialogKey(keyData);
        }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (e.KeyData is Keys.Tab or (Keys.Shift | Keys.Tab))
                return MoveBetweenConnectButtons(e.KeyData == Keys.Tab);
            return base.ProcessDataGridViewKey(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            // Activation is handled on key-down; the native button cell must not invoke it again on release.
            if (e.Modifiers == Keys.None && e.KeyCode is Keys.Enter or Keys.Space)
            {
                e.SuppressKeyPress = true;
                return;
            }
            base.OnKeyUp(e);
        }

        private void FocusConnect(int row)
        {
            if (row < 0 || row >= Rows.Count || !Columns.Contains("Connect"))
                return;

            CurrentCell = Rows[row].Cells["Connect"];
            Invalidate();
        }

        private bool MoveBetweenConnectButtons(bool forward)
        {
            _keyboardNavigation = true;
            int next = CurrentCell?.RowIndex ?? (forward ? -1 : Rows.Count);
            if (CurrentCell is null || CurrentCell.OwningColumn?.Name == "Connect" || !forward)
                next += forward ? 1 : -1;
            if (next >= 0 && next < Rows.Count)
            {
                FocusConnect(next);
                return true;
            }
            return FindForm()?.SelectNextControl(this, forward, true, true, true) == true;
        }

        private void ActivateFocusedConnect()
        {
            if (!Enabled || CurrentCell is null)
                return;

            int row = CurrentCell.RowIndex;
            FocusConnect(row);
            ConnectRequested?.Invoke(this, new DataGridViewCellEventArgs(CurrentCell!.ColumnIndex, row));
        }
        #endregion
    }
}
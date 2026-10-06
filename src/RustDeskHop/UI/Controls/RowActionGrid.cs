namespace RustDeskHop.UI.Controls
{
    internal sealed class RowActionGrid : DataGridView
    {
        #region Constructors and Destructors
        internal RowActionGrid()
        {
            DoubleBuffered = true;
        }
        #endregion

        #region Methods
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (TryActivateCurrentAction(keyData))
                return true;

            return base.ProcessDialogKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (TryActivateCurrentAction(e.KeyData))
            {
                e.SuppressKeyPress = true;
                return;
            }
            base.OnKeyDown(e);
        }

        private bool TryActivateCurrentAction(Keys keyData)
        {
            if (keyData is not (Keys.Enter or Keys.Space) || CurrentCell is not RowActionCell cell)
                return false;

            if (Enabled && cell.ActionEnabled)
                OnCellContentClick(new DataGridViewCellEventArgs(cell.ColumnIndex, cell.RowIndex));
            return true;
        }
        #endregion
    }
}
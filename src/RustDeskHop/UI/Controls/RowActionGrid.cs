using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class RowActionGrid : DataGridView
    {
        #region Constants and Fields
        private readonly GridInteractionMotion _motion;
        #endregion

        #region Constructors and Destructors
        internal RowActionGrid()
        {
            DoubleBuffered = true;
            _motion = new GridInteractionMotion(this,
                                                (row, column) =>
                                                    Rows[row].Cells[column] is RowActionCell { ActionEnabled: true }
                                               );
        }
        #endregion

        #region Methods
        internal float Interaction(int row, int column) => _motion.Cell(row, column);
        internal Color RowColor(int row) => _motion.RowColor(row);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _motion.Dispose();
            base.Dispose(disposing);
        }

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
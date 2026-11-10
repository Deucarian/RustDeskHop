namespace RustDeskHop.UI.Controls
{
    internal class PointerFocusGrid : DataGridView
    {
        #region Properties and Indexers
        protected bool IsPointerFocus { get; private set; }
        #endregion

        #region Methods
        protected override void WndProc(ref Message message)
        {
            // Focus can reveal the old current cell before native hit testing processes the click.
            // Restore the viewport first so that the click still targets the row under the pointer.
            if (message.Msg is 0x0201 or 0x0204 && !ContainsFocus)
            {
                int firstRow = FirstDisplayedScrollingRowIndex;
                int horizontalOffset = HorizontalScrollingOffset;
                IsPointerFocus = true;
                try
                {
                    Focus();
                    if (firstRow >= 0 && firstRow < Rows.Count)
                        FirstDisplayedScrollingRowIndex = firstRow;
                    HorizontalScrollingOffset = horizontalOffset;
                }
                finally
                {
                    IsPointerFocus = false;
                }
            }
            base.WndProc(ref message);
        }
        #endregion
    }
}

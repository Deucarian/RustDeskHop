namespace RustDeskHop.UI.Theme
{
    internal sealed record TransitionRow(object Key, Rectangle Bounds);

    internal sealed class TransitionFrame : IDisposable
    {
        #region Constructors and Destructors
        private TransitionFrame(Bitmap image, Color background, IReadOnlyList<TransitionRow> rows)
        {
            Image = image;
            Background = background;
            Rows = rows;
        }
        #endregion

        #region Properties and Indexers
        internal Bitmap Image { get; }
        internal Color Background { get; }
        internal IReadOnlyList<TransitionRow> Rows { get; }
        #endregion

        #region Methods
        internal static TransitionFrame Capture(Control control)
        {
            Bitmap image = new Bitmap(Math.Max(1, control.Width), Math.Max(1, control.Height));
            try
            {
                control.DrawToBitmap(image, control.ClientRectangle);
                List<TransitionRow> rows = [];
                if (control is DataGridView grid)
                {
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        Rectangle bounds = grid.GetRowDisplayRectangle(row.Index, true);
                        bounds.Intersect(grid.ClientRectangle);
                        if (bounds.Width > 0 && bounds.Height > 0)
                            rows.Add(new TransitionRow(row.Tag ?? row.DataBoundItem ?? "add-row", bounds));
                    }
                }
                else if (control is ListBox list)
                {
                    for (int index = 0; index < list.Items.Count; index++)
                    {
                        Rectangle bounds = list.GetItemRectangle(index);
                        bounds.Intersect(list.ClientRectangle);
                        if (bounds.Width > 0 && bounds.Height > 0)
                            rows.Add(new TransitionRow(list.Items[index], bounds));
                    }
                }
                return new TransitionFrame(image, control.BackColor, rows);
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }

        public void Dispose() => Image.Dispose();
        #endregion
    }
}
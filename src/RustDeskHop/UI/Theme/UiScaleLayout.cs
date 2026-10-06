namespace RustDeskHop.UI.Theme
{
    internal interface IUiScaleAware
    {
        void ApplyUiScale();
    }

    // Immutable logical baselines prevent rounding drift when the slider is moved repeatedly.
    internal sealed class UiScaleLayout
    {
        #region Constants and Fields
        private readonly List<ControlSnapshot> _controls = [];
        #endregion

        #region Constructors and Destructors
        internal UiScaleLayout(Control root)
        {
            Capture(root);
        }
        #endregion

        #region Methods
        internal Action PauseEditing()
        {
            List<Action> restore = [];
            foreach (ControlSnapshot snapshot in _controls)
            {
                if (snapshot.Control is not DataGridView grid)
                    continue;

                bool wasEditing = grid.IsCurrentCellInEditMode;
                DataGridViewCell? cell = grid.CurrentCell;
                DataGridViewEditMode mode = grid.EditMode;
                int selectionStart = (grid.EditingControl as TextBox)?.SelectionStart ?? 0;
                int selectionLength = (grid.EditingControl as TextBox)?.SelectionLength ?? 0;
                // Commit to the existing in-memory draft, never to the settings store.
                grid.EndEdit();
                grid.EditMode = DataGridViewEditMode.EditProgrammatically;
                restore.Add(() =>
                            {
                                if (grid.IsDisposed)
                                    return;

                                grid.EditMode = mode;
                                if (!wasEditing || cell?.DataGridView != grid || grid.CurrentCell != cell)
                                    return;

                                grid.BeginEdit(false);
                                if (grid.EditingControl is TextBox editor)
                                    editor.Select(Math.Min(selectionStart, editor.TextLength), selectionLength);
                            }
                           );
            }
            return () =>
            {
                foreach (Action action in restore)
                {
                    action();
                }
            };
        }

        internal void Apply()
        {
            foreach (ControlSnapshot snapshot in _controls)
            {
                snapshot.Control.SuspendLayout();
            }
            try
            {
                foreach (ControlSnapshot snapshot in _controls)
                {
                    snapshot.Apply();
                }
                foreach (ControlSnapshot snapshot in _controls)
                {
                    if (snapshot.Control is IUiScaleAware aware)
                        aware.ApplyUiScale();
                }
            }
            finally
            {
                foreach (ControlSnapshot snapshot in _controls.AsEnumerable().Reverse())
                {
                    snapshot.Control.ResumeLayout(true);
                    snapshot.Control.Invalidate(true);
                }
            }
        }

        private void Capture(Control control)
        {
            _controls.Add(new ControlSnapshot(control));
            // Data-grid rows and editing controls are dynamic and are scaled by their owning editor.
            if (control is DataGridView)
                return;

            foreach (Control child in control.Controls)
            {
                Capture(child);
            }
        }
        #endregion

        #region Nested Types
        private sealed class ControlSnapshot
        {
            #region Constants and Fields
            private readonly float _dpi;
            private readonly Size _size;
            private readonly Size _minimum;
            private readonly Size _maximum;
            private readonly Padding _padding;
            private readonly Padding _margin;
            private readonly Font _font;
            private readonly float[] _columns;
            private readonly float[] _rows;
            private readonly int _itemHeight;
            #endregion

            #region Constructors and Destructors
            internal ControlSnapshot(Control control)
            {
                Control = control;
                _dpi = control.DeviceDpi / 96F;
                _size = control.Size;
                _minimum = control.MinimumSize;
                _maximum = control.MaximumSize;
                _padding = control.Padding;
                _margin = control.Margin;
                _font = control.Font;
                _columns = control is TableLayoutPanel table ? table.ColumnStyles.Cast<ColumnStyle>()
                    .Select(style => style.SizeType == SizeType.Absolute ? style.Width : -1).ToArray() : [];
                _rows = control is TableLayoutPanel rows ? rows.RowStyles.Cast<RowStyle>()
                    .Select(style => style.SizeType == SizeType.Absolute ? style.Height : -1).ToArray() : [];
                if (control is ListBox list)
                    _itemHeight = list.ItemHeight;
            }
            #endregion

            #region Properties and Indexers
            internal Control Control { get; }
            #endregion

            #region Methods
            internal void Apply()
            {
                float factor = UiScale.Factor(Control) / _dpi;
                int Px(int value) => (int)Math.Round(value * factor);
                Size ScaleSize(Size size) => new Size(Px(size.Width), Px(size.Height));
                Padding ScalePadding(Padding value) =>
                    new Padding(Px(value.Left), Px(value.Top), Px(value.Right), Px(value.Bottom));

                Control.MinimumSize = Size.Empty;
                Control.MaximumSize = ScaleSize(_maximum);
                if (Control.Dock == DockStyle.None)
                    Control.Size = ScaleSize(_size);
                else if (Control.Dock is DockStyle.Top or DockStyle.Bottom)
                    Control.Height = Px(_size.Height);
                else if (Control.Dock is DockStyle.Left or DockStyle.Right)
                    Control.Width = Px(_size.Width);
                Control.MinimumSize = ScaleSize(_minimum);
                Control.Padding = ScalePadding(_padding);
                Control.Margin = ScalePadding(_margin);
                Control.Font = UiScale.FontFor(Control, _font);
                if (Control is TableLayoutPanel table)
                {
                    for (int index = 0; index < _columns.Length; index++)
                    {
                        if (_columns[index] >= 0)
                            table.ColumnStyles[index].Width = _columns[index] * factor;
                    }
                    for (int index = 0; index < _rows.Length; index++)
                    {
                        if (_rows[index] >= 0)
                            table.RowStyles[index].Height = _rows[index] * factor;
                    }
                }
                if (Control is ListBox list)
                    list.ItemHeight = Math.Max(1, Px(_itemHeight));
            }
            #endregion
        }
        #endregion
    }
}
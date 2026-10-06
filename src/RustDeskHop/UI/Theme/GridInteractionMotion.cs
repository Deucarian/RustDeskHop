namespace RustDeskHop.UI.Theme
{
    internal sealed class GridInteractionMotion : IDisposable
    {
        #region Constants and Fields
        private readonly DataGridView _grid;
        private readonly Func<int, int, bool> _isAction;
        private readonly MotionTween _tween;
        private readonly Dictionary<(int Row, int Column), float> _values = [];
        private Dictionary<(int Row, int Column), float> _from = [];
        private Dictionary<(int Row, int Column), float> _targets = [];
        private int _row = -1;
        private int _column = -1;
        private bool _pressed;
        #endregion

        #region Constructors and Destructors
        internal GridInteractionMotion(DataGridView grid, Func<int, int, bool> isAction)
        {
            _grid = grid;
            _isAction = isAction;
            _tween = new MotionTween(Advance);
            grid.MouseMove += (_, e) =>
            {
                DataGridView.HitTestInfo hit = grid.HitTest(e.X, e.Y);
                if (hit.RowIndex == _row && hit.ColumnIndex == _column)
                    return;

                _row = hit.RowIndex;
                _column = hit.ColumnIndex;
                Update();
            };
            grid.MouseLeave += (_, _) =>
            {
                _row = _column = -1;
                SetPressed(false);
            };
            grid.MouseDown += (_, e) => SetPressed(e.Button == MouseButtons.Left);
            grid.MouseUp += (_, _) => SetPressed(false);
            grid.CurrentCellChanged += (_, _) => Update();
            grid.GotFocus += (_, _) => Update();
            grid.LostFocus += (_, _) => SetPressed(false);
            grid.EnabledChanged += (_, _) => Update();
            grid.Scroll += (_, _) => Reset();
            grid.RowsRemoved += (_, _) => Reset();
        }
        #endregion

        #region Methods
        internal float Cell(int row, int column) => _values.GetValueOrDefault((row, column));
        internal Color RowColor(int row, bool selected = false) =>
            UiMotion.Blend(selected ? AppTheme.selection : Color.White, AppTheme.selection, Cell(row, -1));

        private void Update()
        {
            Dictionary<(int Row, int Column), float> targets = [];
            if (_grid.Enabled)
            {
                if (_row >= 0 && _row < _grid.Rows.Count)
                {
                    targets[(_row, -1)] = 1;
                    if (_column >= 0)
                        targets[(_row, _column)] = _pressed && _isAction(_row, _column) ? 1 : .65F;
                }
                if (_grid.Focused && _grid.CurrentCell is DataGridViewCell cell)
                {
                    (int Row, int Column) key = (cell.RowIndex, cell.ColumnIndex);
                    targets[key] = Math.Max(.65F, targets.GetValueOrDefault(key));
                }
            }
            _grid.Cursor = _grid.Enabled && _row >= 0 && _column >= 0
                && _row < _grid.Rows.Count && _isAction(_row, _column) ? Cursors.Hand : Cursors.Default;
            _from = new Dictionary<(int Row, int Column), float>(_values);
            _targets = targets;
            _tween.Reset(0);
            _tween.To(1);
        }

        private void Advance(float progress)
        {
            foreach ((int row, int column) in _from.Keys.Union(_targets.Keys))
            {
                float start = _from.GetValueOrDefault((row, column));
                float end = _targets.GetValueOrDefault((row, column));
                _values[(row, column)] = start + (end - start) * progress;
            }
            if (progress >= 1)
            {
                foreach ((int row, int column) in _values.Where(entry => entry.Value == 0)
                    .Select(entry => entry.Key).ToArray())
                {
                    _values.Remove((row, column));
                }
            }
            _grid.Invalidate();
        }

        private void Reset()
        {
            _row = _column = -1;
            _pressed = false;
            _grid.Cursor = Cursors.Default;
            _values.Clear();
            _from.Clear();
            _targets.Clear();
            _tween.Reset(0);
        }

        private void SetPressed(bool pressed)
        {
            _pressed = pressed;
            Update();
        }

        public void Dispose() => _tween.Dispose();
        #endregion
    }
}
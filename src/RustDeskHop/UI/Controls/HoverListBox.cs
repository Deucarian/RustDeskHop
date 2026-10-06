using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class HoverListBox : ListBox
    {
        #region Constants and Fields
        private readonly MotionTween _motion;
        private readonly Dictionary<int, float> _values = new Dictionary<int, float>();
        private Dictionary<int, float> _from = new Dictionary<int, float>();
        private int _hover = -1;
        #endregion

        #region Constructors and Destructors
        internal HoverListBox(Func<bool>? motionEnabled = null)
        {
            // Invalidate must not erase the native list before its owner-drawn rows are ready.
            // UserPaint stays off so Windows retains selection, scrolling and accessibility.
            SetStyle(ControlStyles.Opaque, true);
            _motion = new MotionTween(AdvanceHover, motionEnabled);
        }
        #endregion

        #region Methods
        internal float HoverAmount(int index) => _values.GetValueOrDefault(index);

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0)
                return;

            // Buffer the native owner-draw callback, including selection/focus-only repaints.
            // Setting DoubleBuffered alone would not buffer a native ListBox.
            using BufferedGraphics buffer = BufferedGraphicsManager.Current.Allocate(e.Graphics, e.Bounds);
            // GDI text/focus drawing ignores GDI+ translations. Give every painter local row coordinates.
            buffer.Graphics.ResetTransform();
            Rectangle bounds = new Rectangle(Point.Empty, e.Bounds.Size);
            using SolidBrush background = new SolidBrush(BackColor);
            buffer.Graphics.FillRectangle(background, bounds);
            using DrawItemEventArgs buffered = new DrawItemEventArgs(buffer.Graphics,
                                                                    e.Font,
                                                                    bounds,
                                                                    e.Index,
                                                                    e.State,
                                                                    e.ForeColor,
                                                                    e.BackColor
                                                                   );
            base.OnDrawItem(buffered);
            buffer.Render(e.Graphics);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            SetHover(Enabled ? IndexFromPoint(e.Location) : -1);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetHover(-1);
            base.OnMouseLeave(e);
        }

        protected override void OnDataSourceChanged(EventArgs e)
        {
            ResetHover();
            base.OnDataSourceChanged(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            if (!Enabled)
                ResetHover();
            base.OnEnabledChanged(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            ResetHover();
            base.OnMouseWheel(e);
            SetHover(Enabled ? IndexFromPoint(e.Location) : -1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _motion.Dispose();
            base.Dispose(disposing);
        }

        private void SetHover(int index)
        {
            if (_hover == index)
                return;

            _from = new Dictionary<int, float>(_values);
            _hover = index;
            Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
            _motion.Reset(0);
            _motion.To(1);
        }

        private void AdvanceHover(float progress)
        {
            foreach (int index in _from.Keys.Append(_hover).Distinct())
            {
                if (index < 0)
                    continue;

                float start = _from.GetValueOrDefault(index);
                float target = index == _hover ? 1 : 0;
                float value = start + (target - start) * progress;
                float previous = HoverAmount(index);
                if (value == 0)
                    _values.Remove(index);
                else
                    _values[index] = value;
                if (value != previous)
                    InvalidateRow(index);
            }
            if (progress >= 1)
                _from.Clear();
        }

        private void InvalidateRow(int index)
        {
            if (!IsHandleCreated || IsDisposed || index < 0 || index >= Items.Count)
                return;

            Rectangle bounds = Rectangle.Intersect(ClientRectangle, GetItemRectangle(index));
            // Invalidate(Rectangle.Empty) means the entire control, not "nothing".
            if (bounds.Width > 0 && bounds.Height > 0)
                Invalidate(bounds);
        }

        private void ResetHover()
        {
            foreach (int index in _values.Keys)
            {
                InvalidateRow(index);
            }
            _values.Clear();
            _from.Clear();
            _hover = -1;
            _motion?.Reset(0);
            Cursor = Cursors.Default;
        }
        #endregion
    }
}
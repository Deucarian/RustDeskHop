using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class HoverListBox : ListBox
    {
        #region Constants and Fields
        private readonly MotionTween _motion;
        private int _hover = -1;
        private int _previous = -1;
        #endregion

        #region Constructors and Destructors
        internal HoverListBox()
        {
            _motion = new MotionTween(_ => Invalidate());
        }
        #endregion

        #region Methods
        internal float HoverAmount(int index) => index == _hover ? _motion.Value
            : index == _previous ? 1 - _motion.Value : 0;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            SetHover(IndexFromPoint(e.Location));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetHover(-1);
            base.OnMouseLeave(e);
        }

        protected override void OnDataSourceChanged(EventArgs e)
        {
            _hover = _previous = -1;
            _motion?.Reset(0);
            Cursor = Cursors.Default;
            base.OnDataSourceChanged(e);
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

            _previous = _hover;
            _hover = index;
            Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
            _motion.Reset(0);
            _motion.To(1);
        }
        #endregion
    }
}
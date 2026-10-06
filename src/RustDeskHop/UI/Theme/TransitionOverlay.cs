using System.Drawing.Imaging;

namespace RustDeskHop.UI.Theme
{
    internal sealed class TransitionOverlay : Control, IMessageFilter
    {
        #region Constants and Fields
        private readonly TransitionFrame _before;
        private readonly TransitionFrame _after;
        private readonly MotionTween _motion;
        private readonly bool _reflow;
        private float _progress;
        #endregion

        #region Constructors and Destructors
        internal TransitionOverlay(TransitionFrame before,
                                   TransitionFrame after,
                                   bool reflow,
                                   Func<bool>? enabled = null)
        {
            _before = before;
            _after = after;
            _reflow = reflow;
            TabStop = false;
            AccessibleRole = AccessibleRole.None;
            SetStyle(ControlStyles.Selectable, false);
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.AllPaintingInWmPaint,
                     true
                    );
            _motion = new MotionTween(value =>
                                     {
                                         _progress = value;
                                         Invalidate();
                                         if (value >= 1)
                                             Finish();
                                     },
                                     enabled
                                    );
        }
        #endregion

        #region Methods
        protected override AccessibleObject CreateAccessibilityInstance() => new OverlayAccessibleObject(this);

        internal static Rectangle ReflowBounds(Rectangle from, Rectangle to, float progress) =>
            new Rectangle(to.X, (int)Math.Round(from.Y + (to.Y - from.Y) * progress), to.Width, to.Height);

        internal void Start()
        {
            Application.AddMessageFilter(this);
            BringToFront();
            _motion.To(1, UiMotion.CONTENT_DURATION);
        }

        internal void Finish()
        {
            if (!IsDisposed)
            {
                Parent?.Controls.Remove(this);
                Dispose();
            }
        }

        public bool PreFilterMessage(ref Message message)
        {
            // Keyboard/scroll interaction sees the real final layout immediately.
            if (message.Msg is >= 0x0100 and <= 0x0109 or 0x020A or 0x020E)
                Finish();
            return false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Never dispatch a click to a different row while it is visibly moving.
            Finish();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(UiMotion.Blend(_before.Background, _after.Background, _progress));
            if (!_reflow || _before.Rows.Count == 0 || _after.Rows.Count == 0)
            {
                Draw(e.Graphics,
                     _before.Image,
                     new Rectangle(Point.Empty, _before.Image.Size),
                     ClientRectangle,
                     1 - _progress
                    );
                Rectangle incoming = ClientRectangle;
                incoming.Y += (int)Math.Round(8 * (1 - _progress));
                Draw(e.Graphics, _after.Image, new Rectangle(Point.Empty, _after.Image.Size), incoming, _progress);
                return;
            }
            foreach (TransitionRow row in _before.Rows.Where(old => !_after.Rows.Any(next => next.Key.Equals(old.Key))))
            {
                Draw(e.Graphics, _before.Image, row.Bounds, row.Bounds, 1 - _progress);
            }
            foreach (TransitionRow row in _after.Rows)
            {
                TransitionRow? previous = _before.Rows.FirstOrDefault(old => old.Key.Equals(row.Key));
                Rectangle destination = previous is null
                    ? ReflowBounds(new Rectangle(row.Bounds.X, row.Bounds.Y + 8, row.Bounds.Width, row.Bounds.Height),
                                   row.Bounds,
                                   _progress
                                  )
                    : ReflowBounds(previous.Bounds, row.Bounds, _progress);
                Draw(e.Graphics, _after.Image, row.Bounds, destination, previous is null ? _progress : 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Application.RemoveMessageFilter(this);
                _motion.Dispose();
                _before.Dispose();
                _after.Dispose();
            }
            base.Dispose(disposing);
        }

        private static void Draw(Graphics graphics, Bitmap image, Rectangle source, Rectangle destination, float alpha)
        {
            using ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(new ColorMatrix { Matrix33 = Math.Clamp(alpha, 0, 1) });
            graphics.DrawImage(image,
                               destination,
                               source.X,
                               source.Y,
                               source.Width,
                               source.Height,
                               GraphicsUnit.Pixel,
                               attributes
                              );
        }
        #endregion

        #region Nested Types
        private sealed class OverlayAccessibleObject(Control owner) : ControlAccessibleObject(owner)
        {
            #region Properties and Indexers
            public override AccessibleStates State => AccessibleStates.Invisible | AccessibleStates.Offscreen;
            public override AccessibleRole Role => AccessibleRole.None;
            public override string? Name { get => ""; set { } }
            #endregion

            #region Methods
            public override int GetChildCount() => 0;
            #endregion
        }
        #endregion
    }
}
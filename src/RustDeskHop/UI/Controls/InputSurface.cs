using System.Drawing.Drawing2D;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class InputSurface : Panel
    {
        #region Constants and Fields
        private readonly Control _editor;
        private readonly InteractionMotion _motion;
        #endregion

        #region Constructors and Destructors
        public InputSurface(Control editor)
        {
            _editor = editor;
            _motion = new InteractionMotion(this, editor);
            AutoSize = true;
            Height = UiMetrics.BUTTON_HEIGHT;
            MinimumSize = new Size(100, UiMetrics.BUTTON_HEIGHT);
            BackColor = Color.White;
            Margin = new Padding(0, 0, 0, UiMetrics.GAP);
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            if (editor is TextBox box)
                box.BorderStyle = BorderStyle.None;
            Controls.Add(editor);
            editor.GotFocus += (_, _) => Invalidate();
            editor.LostFocus += (_, _) => Invalidate();
        }
        #endregion

        #region Methods
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _motion.Dispose();
            base.Dispose(disposing);
        }

        public override Size GetPreferredSize(Size proposedSize) =>
            new Size(UiScale.Pixels(this, 100), UiScale.Pixels(this, UiMetrics.BUTTON_HEIGHT));

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_editor is null)
                return;

            int padding = (int)(12 * UiScale.Factor(this));
            _editor.SetBounds(padding,
                              Math.Max(0, (Height - _editor.PreferredSize.Height) / 2),
                              Math.Max(1, Width - padding * 2),
                              _editor.PreferredSize.Height
                             );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using GraphicsPath shape =
                AppTheme.Round(new RectangleF(.75F, .75F, Width - 1.5F, Height - 1.5F), 7 * UiScale.Factor(this));
            using Pen pen = new Pen(UiMotion.Blend(AppTheme.windowBorder,
                                                  AppTheme.blue,
                                                  ContainsFocus ? 1 : _motion.Value
                                                 )
                                  );
            e.Graphics.DrawPath(pen, shape);
        }
        #endregion
    }
}
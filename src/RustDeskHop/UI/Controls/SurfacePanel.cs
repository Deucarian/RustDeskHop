using System.ComponentModel;
using System.Drawing.Drawing2D;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class SurfacePanel : Panel
    {
        #region Constructors and Destructors
        public SurfacePanel()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }
        #endregion

        #region Properties and Indexers
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool Outlined { get; set; } = true;
        #endregion

        #region Methods
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? AppTheme.canvas);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using GraphicsPath path = AppTheme.Round(new RectangleF(.5F, .5F, Width - 1, Height - 1),
                                                     UiMetrics.RADIUS * UiScale.Factor(this)
                                                    );
            using SolidBrush fill = new SolidBrush(BackColor);
            e.Graphics.FillPath(fill, path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!Outlined)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using GraphicsPath path = AppTheme.Round(new RectangleF(.5F, .5F, Width - 1, Height - 1),
                                                     UiMetrics.RADIUS * UiScale.Factor(this)
                                                    );
            using Pen pen = new Pen(AppTheme.line);
            e.Graphics.DrawPath(pen, path);
        }
        #endregion
    }
}
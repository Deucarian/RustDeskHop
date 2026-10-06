using System.Drawing.Drawing2D;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal static class PageTabPainter
    {
        #region Methods
        internal static void Draw(Graphics graphics,
                                  Rectangle bounds,
                                  Rectangle stripBounds,
                                  float scale,
                                  bool selected,
                                  bool enabled,
                                  float interaction)
        {
            // Every header paints the same strip geometry through its own clip, never a separate tab outline.
            using GraphicsPath outline = CreateOutline(stripBounds, scale);
            Color resting = selected ? Color.White : AppTheme.canvas;
            Color fill = enabled ? UiMotion.Blend(resting,
                                                 UiMotion.ButtonFill(false, true, interaction),
                                                 Math.Min(1, interaction / .65F)
                                                ) : AppTheme.badge;
            using SolidBrush background = new SolidBrush(fill);
            graphics.FillPath(background, outline);
            using Pen border = new Pen(AppTheme.line, scale);
            graphics.DrawPath(border, outline);
            if (bounds.Left > stripBounds.Left)
                graphics.DrawLine(border, bounds.Left, bounds.Top + 8 * scale, bounds.Left, bounds.Bottom - 8 * scale);

            if (selected)
            {
                using SolidBrush indicator = new SolidBrush(enabled ? AppTheme.blue : AppTheme.muted);
                graphics.FillRectangle(indicator,
                                       bounds.Left,
                                       bounds.Bottom - 3 * scale,
                                       bounds.Width,
                                       3 * scale
                                      );
            }
        }

        private static GraphicsPath CreateOutline(Rectangle bounds, float scale)
        {
            float inset = scale / 2;
            float radius = 7 * scale;
            float left = bounds.Left + inset;
            float right = bounds.Right - inset;
            float top = bounds.Top + inset;
            float bottom = bounds.Bottom - inset;
            GraphicsPath outline = new GraphicsPath();
            outline.AddLine(left, bottom, left, top + radius);
            outline.AddArc(left, top, radius * 2, radius * 2, 180, 90);
            outline.AddArc(right - radius * 2, top, radius * 2, radius * 2, 270, 90);
            outline.AddLine(right, top + radius, right, bottom);
            outline.CloseFigure();
            return outline;
        }
        #endregion
    }
}
using System.Drawing.Drawing2D;

namespace RustDeskHop.UI.Theme
{
    internal enum UiGlyph
    {
        NONE,
        MONITOR,
        GLOBE,
        NETWORK,
        PLUS,
        TRASH,
        ARROW,
        INFO,
        CHECK,
        SETTINGS
    }

    internal static class GlyphPainter
    {
        #region Methods
        internal static void Draw(Graphics graphics, UiGlyph glyph, RectangleF bounds, Color color)
        {
            if (glyph == UiGlyph.NONE)
                return;

            GraphicsState state = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TranslateTransform(bounds.X, bounds.Y);
            graphics.ScaleTransform(bounds.Width / 24F, bounds.Height / 24F);
            using Pen pen = new Pen(color, 1.65F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            switch (glyph)
            {
                case UiGlyph.SETTINGS:
                    graphics.DrawEllipse(pen, 5, 5, 14, 14);
                    graphics.DrawEllipse(pen, 9, 9, 6, 6);
                    for (int tooth = 0; tooth < 8; tooth++)
                    {
                        double angle = tooth * Math.PI / 4;
                        graphics.DrawLine(pen,
                                          12 + (float)Math.Cos(angle) * 7,
                                          12 + (float)Math.Sin(angle) * 7,
                                          12 + (float)Math.Cos(angle) * 10,
                                          12 + (float)Math.Sin(angle) * 10
                                         );
                    }
                    break;
                case UiGlyph.MONITOR:
                    using (GraphicsPath shape = AppTheme.Round(new RectangleF(2, 3, 20, 14), 1.5F))
                        graphics.DrawPath(pen, shape);
                    graphics.DrawLine(pen, 12, 17, 12, 21);
                    graphics.DrawLine(pen, 7, 21, 17, 21);
                    break;
                case UiGlyph.GLOBE:
                    graphics.DrawEllipse(pen, 2, 2, 20, 20);
                    graphics.DrawEllipse(pen, 7, 2, 10, 20);
                    graphics.DrawLine(pen, 3, 8, 21, 8);
                    graphics.DrawLine(pen, 3, 16, 21, 16);
                    break;
                case UiGlyph.NETWORK:
                    graphics.DrawRectangle(pen, 9, 2, 6, 5);
                    graphics.DrawRectangle(pen, 2, 17, 6, 5);
                    graphics.DrawRectangle(pen, 16, 17, 6, 5);
                    graphics.DrawLine(pen, 12, 7, 12, 12);
                    graphics.DrawLines(pen, [new Point(5, 17), new Point(5, 12), new Point(19, 12), new Point(19, 17)]);
                    break;
                case UiGlyph.PLUS:
                    graphics.DrawLine(pen, 12, 3, 12, 21);
                    graphics.DrawLine(pen, 3, 12, 21, 12);
                    break;
                case UiGlyph.TRASH:
                    graphics.DrawLine(pen, 3, 6, 21, 6);
                    graphics.DrawLines(pen, [new Point(5, 6), new Point(6, 22), new Point(18, 22), new Point(19, 6)]);
                    graphics.DrawLines(pen, [new Point(8, 6), new Point(8, 2), new Point(16, 2), new Point(16, 6)]);
                    break;
                case UiGlyph.ARROW:
                    graphics.DrawLine(pen, 3, 12, 21, 12);
                    graphics.DrawLines(pen, [new Point(15, 6), new Point(21, 12), new Point(15, 18)]);
                    break;
                case UiGlyph.INFO:
                    graphics.DrawEllipse(pen, 2, 2, 20, 20);
                    graphics.DrawLine(pen, 12, 11, 12, 17);
                    using (SolidBrush brush = new SolidBrush(color))
                        graphics.FillEllipse(brush, 11, 6, 2, 2);
                    break;
                case UiGlyph.CHECK:
                    graphics.DrawLines(pen, [new Point(5, 12), new Point(10, 17), new Point(20, 7)]);
                    break;
            }

            graphics.Restore(state);
        }
        #endregion
    }
}
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal static class ComputerIdentityPainter
    {
        #region Methods
        internal static string DisplayId(string id)
        {
            if (id.Length == 9 && id.All(char.IsAsciiDigit))
                return $"{id[..3]} {id[3..6]} {id[6..]}";

            return id;
        }

        internal static void Draw(Graphics graphics,
                                  Control? owner,
                                  Rectangle bounds,
                                  string name,
                                  string id,
                                  float scale)
        {
            int Px(int value) => (int)Math.Round(value * scale);
            GlyphPainter.Draw(graphics,
                              UiGlyph.MONITOR,
                              new Rectangle(bounds.Left + Px(12),
                                            bounds.Top + (bounds.Height - Px(26)) / 2,
                                            Px(26),
                                            Px(26)
                                           ),
                              AppTheme.muted
                             );
            int width = Math.Max(1, bounds.Width - Px(UiMetrics.IDENTITY_INSET + UiMetrics.CELL_INSET));
            int nameHeight = TextRenderer.MeasureText(name,
                                                     UiScale.FontFor(owner, AppTheme.strong),
                                                     new Size(width, int.MaxValue),
                                                     TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix
                                                    ).Height;
            int idHeight = UiScale.FontFor(owner, AppTheme.small).Height;
            int top = bounds.Top + Math.Max(Px(8), (bounds.Height - nameHeight - idHeight - Px(3)) / 2);
            Rectangle nameBounds = new Rectangle(bounds.Left + Px(UiMetrics.IDENTITY_INSET), top, width, nameHeight);
            TextRenderer.DrawText(graphics,
                                  name,
                                  UiScale.FontFor(owner, AppTheme.strong),
                                  nameBounds,
                                  AppTheme.ink,
                                  TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix
                                 );
            TextRenderer.DrawText(graphics,
                                  DisplayId(id),
                                  UiScale.FontFor(owner, AppTheme.small),
                                  new Rectangle(nameBounds.Left, nameBounds.Bottom + Px(3), width, idHeight),
                                  AppTheme.muted,
                                  TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
                                 );
        }
        #endregion
    }
}
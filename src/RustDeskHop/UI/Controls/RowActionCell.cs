using System.Drawing.Drawing2D;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class RowActionCell : DataGridViewButtonCell
    {
        #region Properties and Indexers
        internal bool Quiet { get; set; }
        internal bool AddAction { get; set; }
        internal bool ActionEnabled { get; set; } = true;
        #endregion

        #region Methods
        public override object Clone()
        {
            RowActionCell clone = (RowActionCell)base.Clone();
            clone.Quiet = Quiet;
            clone.AddAction = AddAction;
            clone.ActionEnabled = ActionEnabled;
            return clone;
        }

        protected override void Paint(Graphics graphics,
                                      Rectangle clipBounds,
                                      Rectangle cellBounds,
                                      int rowIndex,
                                      DataGridViewElementStates elementState,
                                      object? value,
                                      object? formattedValue,
                                      string? errorText,
                                      DataGridViewCellStyle cellStyle,
                                      DataGridViewAdvancedBorderStyle advancedBorderStyle,
                                      DataGridViewPaintParts paintParts)
        {
            base.Paint(graphics,
                       clipBounds,
                       cellBounds,
                       rowIndex,
                       elementState,
                       value,
                       formattedValue,
                       errorText,
                       cellStyle,
                       advancedBorderStyle,
                       paintParts & (DataGridViewPaintParts.Background | DataGridViewPaintParts.Border
                                     | DataGridViewPaintParts.SelectionBackground)
                      );
            float scale = UiScale.Factor(DataGridView);
            int Px(int logical) => (int)Math.Round(logical * scale);
            bool enabled = ActionEnabled && DataGridView?.Enabled == true;
            float interaction = (DataGridView as RowActionGrid)?.Interaction(rowIndex, ColumnIndex) ?? 0;
            Rectangle backgroundBounds = new Rectangle(cellBounds.X,
                                                       cellBounds.Y,
                                                       cellBounds.Width,
                                                       Math.Max(0, cellBounds.Height - 1)
                                                      );
            Color rowColor = (DataGridView as RowActionGrid)?.RowColor(rowIndex) ?? Color.White;
            using (SolidBrush rowFill = new SolidBrush(rowColor))
                graphics.FillRectangle(rowFill, Rectangle.Intersect(clipBounds, backgroundBounds));
            Rectangle button = new Rectangle(cellBounds.Left + Px(4),
                                             cellBounds.Top + (cellBounds.Height - Px(UiMetrics.BUTTON_HEIGHT)) / 2,
                                             cellBounds.Width - Px(8),
                                             Px(UiMetrics.BUTTON_HEIGHT)
                                            );
            GraphicsState state = graphics.Save();
            graphics.SetClip(Rectangle.Intersect(clipBounds, cellBounds));
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (!Quiet || interaction > 0)
            {
                using GraphicsPath shape = AppTheme.Round(button, Px(7));
                using SolidBrush fill = new SolidBrush(UiMotion.ButtonFill(false, enabled, interaction));
                using Pen border = new Pen(enabled ? AppTheme.blue : AppTheme.line, scale);
                graphics.FillPath(fill, shape);
                if (!Quiet)
                    graphics.DrawPath(border, shape);
            }
            if (AddAction)
            {
                GlyphPainter.Draw(graphics,
                                  UiGlyph.PLUS,
                                  new Rectangle(cellBounds.Left + Px(12),
                                                cellBounds.Top + (cellBounds.Height - Px(24)) / 2,
                                                Px(24),
                                                Px(24)
                                               ),
                                  enabled ? AppTheme.blue : AppTheme.muted
                                 );
                button.X = cellBounds.Left + Px(UiMetrics.IDENTITY_INSET);
                button.Width = Math.Max(1, cellBounds.Right - button.Left - Px(4));
            }
            TextRenderer.DrawText(graphics,
                                  AddAction ? "Add computer" : formattedValue?.ToString(),
                                  cellStyle.Font,
                                  button,
                                  enabled && (!Quiet || AddAction) ? AppTheme.blue : AppTheme.muted,
                                  (AddAction ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter)
                                  | TextFormatFlags.VerticalCenter
                                  | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
                                 );
            if (DataGridView?.Focused == true && DataGridView.CurrentCell == this)
            {
                ControlPaint.DrawFocusRectangle(graphics,
                                                Rectangle.Inflate(button, -Px(3), -Px(3)),
                                                AppTheme.blue,
                                                Color.White
                                               );
            }
            graphics.Restore(state);
        }
        #endregion
    }
}
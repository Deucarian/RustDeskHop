using System.Drawing.Drawing2D;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class ComputerEditorCell : DataGridViewTextBoxCell
    {
        #region Methods
        public override void PositionEditingControl(bool setLocation,
                                                    bool setSize,
                                                    Rectangle cellBounds,
                                                    Rectangle cellClip,
                                                    DataGridViewCellStyle cellStyle,
                                                    bool singleVerticalBorderAdded,
                                                    bool singleHorizontalBorderAdded,
                                                    bool isFirstDisplayedColumn,
                                                    bool isFirstDisplayedRow)
        {
            // Native fill columns can briefly be narrower than their insets during live resize.
            // Keep a valid text-editing rectangle throughout that transition, not only at its final size.
            DataGridViewCellStyle editingStyle = cellStyle;
            if (cellBounds.Width <= cellStyle.Padding.Horizontal + 12)
            {
                editingStyle = cellStyle.Clone();
                editingStyle.Padding = new Padding(0, cellStyle.Padding.Top, 0, cellStyle.Padding.Bottom);
                editingStyle.WrapMode = DataGridViewTriState.False;
            }
            base.PositionEditingControl(setLocation,
                                        setSize,
                                        cellBounds,
                                        cellClip,
                                        editingStyle,
                                        singleVerticalBorderAdded,
                                        singleHorizontalBorderAdded,
                                        isFirstDisplayedColumn,
                                        isFirstDisplayedRow
                                       );
        }

        public override void InitializeEditingControl(int rowIndex,
                                                      object? initialFormattedValue,
                                                      DataGridViewCellStyle dataGridViewCellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
            if (DataGridView?.EditingControl is not TextBox editor)
                return;

            editor.AccessibleName = OwningColumn?.HeaderText;
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
                       DataGridViewPaintParts.Background | DataGridViewPaintParts.Border
                      );
            if (OwningRow is null)
                return;

            bool name = OwningColumn?.Name == "ComputerName";
            bool saved = OwningRow.Cells["RustDeskId"].ReadOnly;
            float scale = UiScale.Factor(DataGridView);
            int Px(int logical) => (int)Math.Round(logical * scale);
            GraphicsState state = graphics.Save();
            graphics.SetClip(Rectangle.Intersect(clipBounds, cellBounds));
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (saved && name)
            {
                ComputerIdentityPainter.Draw(graphics,
                                             DataGridView,
                                             cellBounds,
                                             value?.ToString() ?? "",
                                             OwningRow.Cells["RustDeskId"].Value?.ToString() ?? "",
                                             scale
                                            );
            }
            else if (!saved)
            {
                if (name)
                {
                    GlyphPainter.Draw(graphics,
                                      UiGlyph.MONITOR,
                                      new Rectangle(cellBounds.Left + Px(12),
                                                    cellBounds.Top + (cellBounds.Height - Px(26)) / 2,
                                                    Px(26),
                                                    Px(26)
                                                   ),
                                      AppTheme.muted
                                     );
                }
                Rectangle field = new Rectangle(cellBounds.Left + Px(name ? UiMetrics.IDENTITY_INSET : 4),
                                                cellBounds.Top + (cellBounds.Height - Px(36)) / 2,
                                                cellBounds.Width - Px(name ? UiMetrics.IDENTITY_INSET + 8 : 12),
                                                Px(36)
                                               );
                using GraphicsPath outline = AppTheme.Round(field, Px(7));
                using Pen border = new Pen(IsInEditMode ? AppTheme.blue : AppTheme.windowBorder, scale);
                graphics.DrawPath(border, outline);
                if (!IsInEditMode)
                {
                    string text = value?.ToString() ?? "";
                    TextRenderer.DrawText(graphics,
                                          text.Length == 0 ? name ? "Computer name" : "RustDesk ID" : text,
                                          UiScale.FontFor(DataGridView, AppTheme.body),
                                          Rectangle.Inflate(field, -Px(8), 0),
                                          text.Length == 0 ? AppTheme.muted : AppTheme.ink,
                                          TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
                                          | TextFormatFlags.NoPrefix
                                         );
                }
            }
            if (saved && name && DataGridView?.Focused == true && DataGridView.CurrentCell == this)
            {
                ControlPaint.DrawFocusRectangle(graphics,
                                                Rectangle.Inflate(cellBounds, -Px(4), -Px(4)),
                                                AppTheme.blue,
                                                Color.White
                                               );
            }
            graphics.Restore(state);
        }
        #endregion
    }
}
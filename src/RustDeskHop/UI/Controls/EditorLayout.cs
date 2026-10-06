using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal static class EditorLayout
    {
        #region Methods
        internal static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
        {
            AppTheme.StyleEditor(control);
            control.AccessibleName = label;
            layout.Controls.Add(new Label
                                {
                                    Text = label, AutoSize = true, Anchor = AnchorStyles.Left,
                                    Margin = new Padding(0, 0, UiMetrics.INSET, UiMetrics.GAP),
                                    ForeColor = AppTheme.muted
                                },
                                0,
                                row
                               );
            control = new InputSurface(control);
            control.Dock = DockStyle.Fill;
            layout.Controls.Add(control, 1, row);
        }
        #endregion
    }
}
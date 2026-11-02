using System.Drawing.Drawing2D;

namespace RustDeskHop.UI.Theme
{
    internal static class AppTheme
    {
        #region Constants and Fields
        internal static readonly Color canvas = Color.FromArgb(246, 248, 251);
        internal static readonly Color ink = Color.FromArgb(21, 27, 38);
        internal static readonly Color muted = Color.FromArgb(91, 105, 125);
        internal static readonly Color line = Color.FromArgb(224, 229, 236);
        internal static readonly Color windowBorder = Color.FromArgb(199, 208, 220);
        internal static readonly Color blue = Color.FromArgb(0, 105, 245);
        internal static readonly Color selection = Color.FromArgb(239, 245, 253);
        internal static readonly Color badge = Color.FromArgb(240, 242, 246);
        internal static readonly Font body = new Font("Segoe UI", 10.5F);
        internal static readonly Font heading = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
        internal static readonly Font strong = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
        internal static readonly Font small = new Font("Segoe UI", 9.5F);
        #endregion

        #region Methods
        internal static GraphicsPath Round(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 0)
                return path;

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal static Label Label(string text, Font? font = null, Color? color = null) => new Label()
        {
            Text = text,
            Font = font ?? body,
            ForeColor = color ?? ink,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };

        internal static void StyleEditor(Control control)
        {
            control.Font = body;
            control.Margin = new Padding(0, 5, 0, 12);
            control.AccessibleName ??= control.Name;
            if (control is TextBox box)
                box.BorderStyle = BorderStyle.FixedSingle;
            if (control is ComboBox combo)
                combo.FlatStyle = FlatStyle.Flat;
        }
        #endregion
    }
}
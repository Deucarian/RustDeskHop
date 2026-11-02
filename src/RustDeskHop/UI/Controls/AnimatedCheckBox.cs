using System.Windows.Forms.VisualStyles;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class AnimatedCheckBox : CheckBox
    {
        #region Constants and Fields
        private readonly InteractionMotion _motion;
        #endregion

        #region Constructors and Destructors
        internal AnimatedCheckBox()
        {
            _motion = new InteractionMotion(this);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }
        #endregion

        #region Methods
        protected override void OnPaint(PaintEventArgs e)
        {
            if (SystemInformation.HighContrast)
            {
                base.OnPaint(e);
                return;
            }
            e.Graphics.Clear(UiMotion.Blend(BackColor, AppTheme.selection, _motion.Value));
            CheckBoxState state = !Enabled
                ? Checked ? CheckBoxState.CheckedDisabled : CheckBoxState.UncheckedDisabled
                : Checked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal;
            Size glyph = CheckBoxRenderer.GetGlyphSize(e.Graphics, state);
            CheckBoxRenderer.DrawCheckBox(e.Graphics, new Point(0, (Height - glyph.Height) / 2), state);
            Rectangle text = new Rectangle(glyph.Width + UiScale.Pixels(this, 5),
                                           0,
                                           Math.Max(1, Width - glyph.Width - UiScale.Pixels(this, 5)),
                                           Height
                                          );
            TextRenderer.DrawText(e.Graphics,
                                  Text,
                                  Font,
                                  text,
                                  Enabled ? ForeColor : AppTheme.muted,
                                  TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
                                 );
            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(e.Graphics, text);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _motion.Dispose();
            base.Dispose(disposing);
        }
        #endregion
    }
}
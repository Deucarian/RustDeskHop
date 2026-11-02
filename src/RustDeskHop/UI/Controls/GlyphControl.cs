using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class GlyphControl : Control
    {
        #region Constructors and Destructors
        public GlyphControl(UiGlyph glyph)
        {
            Glyph = glyph;
            ForeColor = AppTheme.muted;
            TabStop = false;
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.SupportsTransparentBackColor,
                     true
                    );
            BackColor = Color.Transparent;
        }
        #endregion

        #region Properties and Indexers
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility
                                                                      .Hidden
                                                              )]
        internal UiGlyph Glyph { get; set; }
        #endregion

        #region Methods
        protected override void OnPaint(PaintEventArgs e) =>
            GlyphPainter.Draw(e.Graphics, Glyph, ClientRectangle, ForeColor);
        #endregion
    }
}
using System.ComponentModel;
using System.Drawing.Drawing2D;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class ModernButton : Button
    {
        #region Constants and Fields
        private readonly InteractionMotion _motion;
        #endregion

        #region Constructors and Destructors
        public ModernButton()
        {
            _motion = new InteractionMotion(this);
            Font = AppTheme.body;
            ForeColor = AppTheme.ink;
            Cursor = Cursors.Hand;
            Height = UiMetrics.BUTTON_HEIGHT;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Margin = new Padding(0, 0, UiMetrics.GAP, 0);
        }
        #endregion

        #region Properties and Indexers
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool Primary { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool Quiet { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal UiGlyph Glyph { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool GlyphAfter { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool Accent { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool AlignLeft { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool TabSegment { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool SelectedTab { get; set; }
        #endregion

        #region Methods
        protected override AccessibleObject CreateAccessibilityInstance() =>
            TabSegment ? new SegmentAccessibleObject(this) : base.CreateAccessibilityInstance();

        protected override bool IsInputKey(Keys keyData) =>
            (TabSegment && keyData is Keys.Left or Keys.Right) || base.IsInputKey(keyData);

        public override Size GetPreferredSize(Size proposedSize) =>
            new Size(Math.Max(MinimumSize.Width,
                              TextRenderer.MeasureText(Text, Font).Width
                              + (int)Math.Round((Glyph == UiGlyph.NONE ? 28 : 52) * UiScale.Factor(this))
                             ),
                     Math.Max((int)Math.Round(UiMetrics.BUTTON_HEIGHT * UiScale.Factor(this)),
                              Font.Height + (int)Math.Round(12 * UiScale.Factor(this))
                             )
                    );

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _motion.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            float scale = UiScale.Factor(this);
            e.Graphics.Clear(Parent?.BackColor ?? AppTheme.canvas);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = UiMotion.ButtonFill(Primary, Enabled, _motion.Value, TabSegment && SelectedTab);
            if (Quiet && !Primary)
                fill = UiMotion.Blend(Parent?.BackColor ?? AppTheme.canvas, fill, _motion.Value);
            Color ink = !Enabled ? AppTheme.muted : Primary ? Color.White
                : Accent ? AppTheme.blue : Quiet ? AppTheme.muted : ForeColor;
            using GraphicsPath shape =
                AppTheme.Round(new RectangleF(.75F, .75F, Width - 1.5F, Height - 1.5F), 7 * scale);
            using SolidBrush brush = new SolidBrush(fill);
            using Pen border = new Pen(Primary && Enabled ? fill : Accent && Enabled ? AppTheme.blue : AppTheme.line,
                                      scale
                                     );
            if (TabSegment)
            {
                Rectangle strip = Parent?.ClientRectangle ?? ClientRectangle;
                strip.Offset(-Left, -Top);
                PageTabPainter.Draw(e.Graphics, ClientRectangle, strip, scale, SelectedTab, Enabled, _motion.Value);
            }
            else
            {
                e.Graphics.FillPath(brush, shape);
                if (!Quiet || Primary)
                    e.Graphics.DrawPath(border, shape);
            }
            int textWidth = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            float iconSize = 18 * scale;
            float gap = Glyph == UiGlyph.NONE ? 0 : 8 * scale;
            float total = textWidth + (Glyph == UiGlyph.NONE ? 0 : iconSize + gap);
            float start = AlignLeft ? UiMetrics.CELL_INSET * scale : (Width - total) / 2;
            if (Glyph != UiGlyph.NONE)
            {
                GlyphPainter.Draw(e.Graphics,
                                  Glyph,
                                  new RectangleF(GlyphAfter ? start + textWidth + gap : start,
                                                 (Height - iconSize) / 2,
                                                 iconSize,
                                                 iconSize
                                                ),
                                  ink
                                 );
            }
            float textX = start + (Glyph != UiGlyph.NONE && !GlyphAfter ? iconSize + gap : 0);
            TextRenderer.DrawText(e.Graphics,
                                  Text,
                                  Font,
                                  new Rectangle((int)textX, 0, textWidth + 2, Height),
                                  ink,
                                  TextFormatFlags.Left
                                  | TextFormatFlags.VerticalCenter
                                  | TextFormatFlags.SingleLine
                                  | TextFormatFlags.NoPadding
                                 );
            if (Focused && ShowFocusCues)
            {
                using Pen focus = new Pen(Primary ? Color.White : AppTheme.blue, scale)
                {
                    DashStyle = DashStyle.Dot
                };
                using GraphicsPath ring =
                    AppTheme.Round(new RectangleF(4 * scale, 4 * scale, Width - 8 * scale, Height - 8 * scale),
                                   4 * scale
                                  );
                e.Graphics.DrawPath(focus, ring);
            }
        }
        #endregion

        #region Nested Types
        private sealed class SegmentAccessibleObject(ModernButton owner) : ControlAccessibleObject(owner)
        {
            #region Properties and Indexers
            public override AccessibleStates State => base.State
                | (owner.TabSegment && owner.SelectedTab ? AccessibleStates.Selected : AccessibleStates.None);
            public override string DefaultAction => "Select";
            #endregion

            #region Methods
            public override void DoDefaultAction() => owner.PerformClick();
            #endregion
        }
        #endregion
    }
}
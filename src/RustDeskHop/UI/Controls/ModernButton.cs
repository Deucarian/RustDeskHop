using System.ComponentModel;
using System.Drawing.Drawing2D;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class ModernButton : Button
    {
        #region Constants and Fields
        private bool _hover;
        private bool _pressed;
        #endregion

        #region Constructors and Destructors
        public ModernButton()
        {
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

        public override Size GetPreferredSize(Size proposedSize) =>
            new Size(Math.Max(MinimumSize.Width,
                              TextRenderer.MeasureText(Text, Font).Width
                              + (int)Math.Round((Glyph == UiGlyph.NONE ? 28 : 52) * UiScale.Factor(this))
                             ),
                     Math.Max((int)Math.Round(UiMetrics.BUTTON_HEIGHT * UiScale.Factor(this)),
                              Font.Height + (int)Math.Round(12 * UiScale.Factor(this))
                             )
                    );

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
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
            Color fill = !Enabled
                ? Color.FromArgb(239, 242, 246)
                :
                Primary
                    ?
                    (_pressed ? Color.FromArgb(0, 77, 196) : _hover ? Color.FromArgb(0, 91, 224) : AppTheme.blue)
                    : (_pressed ? AppTheme.selection : _hover ? Color.FromArgb(244, 248, 253) : Color.White);
            if (Quiet && !Primary && !_hover && !_pressed)
                fill = Parent?.BackColor ?? AppTheme.canvas;
            if (TabSegment)
                fill = SelectedTab ? Color.White : _hover ? AppTheme.selection : Parent?.BackColor ?? AppTheme.canvas;
            Color ink = !Enabled ? AppTheme.muted : Primary ? Color.White
                : Accent ? AppTheme.blue : Quiet ? AppTheme.muted : ForeColor;
            using GraphicsPath shape =
                AppTheme.Round(new RectangleF(.75F, .75F, Width - 1.5F, Height - 1.5F), 7 * scale);
            using SolidBrush brush = new SolidBrush(fill);
            using Pen border = new Pen(Primary && Enabled ? fill : Accent && Enabled ? AppTheme.blue : AppTheme.line,
                                      scale
                                     );
            e.Graphics.FillPath(brush, shape);
            if (TabSegment ? SelectedTab : !Quiet || Primary)
                e.Graphics.DrawPath(border, shape);
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
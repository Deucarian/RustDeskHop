using RustDeskHop.Branding;
using RustDeskHop.UI.Controls;

namespace RustDeskHop.UI.Theme
{
    internal abstract class BrandedForm : Form
    {
        #region Constants and Fields
        private readonly Panel _frame = new Panel { Name = "WindowFrame", Dock = DockStyle.Fill };
        private UiScaleState _scaleState = new UiScaleState(100);
        private bool _ownsScaleState = true;
        private UiScaleLayout? _scaleLayout;
        private SizeF _logicalClientSize;
        private bool _applyingScale;
        #endregion

        #region Constructors and Destructors
        protected BrandedForm()
        {
            Icon = AppBranding.Icon;
            ShowIcon = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = AppTheme.body;
            BackColor = AppTheme.canvas;
            ForeColor = AppTheme.ink;

            // Keep the actual non-client area. Windows owns caption buttons, hit testing,
            // accessibility, snap layouts, system menus and per-monitor frame sizing.
            Padding = Padding.Empty;
            DoubleBuffered = true;
            _frame.Controls.Add(WindowContent);
            Controls.Add(_frame);
        }
        #endregion

        #region Properties and Indexers
        internal UiScaleState ScaleState => _scaleState;
        protected bool IsApplyingUiScale => _applyingScale;
        protected Panel WindowContent { get; } = new Panel()
        {
            Name = "WindowContent",
            Dock = DockStyle.Fill
        };
        #endregion

        #region Methods
        internal void UseScaleState(UiScaleState state)
        {
            if (_scaleLayout is not null)
                throw new InvalidOperationException("Choose the shared UI scale before showing the window.");

            if (_ownsScaleState)
                _scaleState.Dispose();
            _scaleState = state;
            _ownsScaleState = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (Owner is BrandedForm owner && _scaleLayout is null)
                UseScaleState(owner.ScaleState);
            base.OnLoad(e);
            if (_scaleLayout is not null)
                return;

            float dpi = DeviceDpi / 96F;
            _logicalClientSize = new SizeF(ClientSize.Width / dpi, ClientSize.Height / dpi);
            _scaleLayout = new UiScaleLayout(WindowContent);
            if (_ownsScaleState)
                _scaleState.SetPercent(UiScaleState.DEFAULT_PERCENT);
            _scaleState.Changed += OnUiScaleChanged;
            ApplyUiScale();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            base.OnDpiChanged(e);
            ApplyUiScale();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _scaleState.Changed -= OnUiScaleChanged;
            base.Dispose(disposing);
            if (disposing && _ownsScaleState)
                _scaleState.Dispose();
        }

        private void OnUiScaleChanged(object? sender, EventArgs e) => ApplyUiScale();

        private void ApplyUiScale()
        {
            if (_scaleLayout is null || _applyingScale || IsDisposed)
                return;

            _applyingScale = true;
            Action restoreEditing = _scaleLayout.PauseEditing();
            SuspendLayout();
            try
            {
                float factor = UiScale.Factor(this);
                MinimumSize = Size.Empty;
                MaximumSize = Size.Empty;
                ClientSize = new Size((int)Math.Round(_logicalClientSize.Width * factor),
                                      (int)Math.Round(_logicalClientSize.Height * factor)
                                     );
                _scaleLayout.Apply();
                WindowContent.PerformLayout();
                MinimumSize = Size;
                MaximumSize = Size;
            }
            finally
            {
                ResumeLayout(true);
                _applyingScale = false;
            }
            if (!IsDisposed)
                restoreEditing();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyWindowTheme();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            ApplyWindowTheme(true);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            ApplyWindowTheme(false);
        }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);

            // A theme/accessibility change must not leave forced caption colours behind.
            if (message.Msg is 0x001A or 0x031A)
                ApplyWindowTheme();
        }

        private void ApplyWindowTheme(bool? active = null)
        {
            if (IsHandleCreated)
                NativeWindowTheme.Apply(Handle, active ?? ActiveForm == this);
        }
        #endregion
    }
}
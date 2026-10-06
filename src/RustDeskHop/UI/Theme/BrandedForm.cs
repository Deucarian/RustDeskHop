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
        private UiScaleSlider? _scaleSlider;
        private SizeF _logicalMinimum;
        private float _appliedFactor = 1F;
        private bool _applyingScale;
        #endregion

        #region Constructors and Destructors
        protected BrandedForm()
        {
            Icon = AppBranding.Icon;
            ShowIcon = true;
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
        protected int ScaleControlHeight => _scaleSlider?.Height ?? 0;
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
            Size chrome = Size - ClientSize;
            _logicalMinimum = new SizeF(Math.Max(0, MinimumSize.Width - chrome.Width) / dpi,
                                        Math.Max(0, MinimumSize.Height - chrome.Height) / dpi
                                       );
            _scaleLayout = new UiScaleLayout(WindowContent);
            _scaleSlider = new UiScaleSlider(_scaleState) { TabIndex = 10 };
            _frame.Controls.Add(_scaleSlider);
            _scaleState.Changed += OnUiScaleChanged;
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + _scaleSlider.Height);
            ApplyUiScale();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            ApplyUiScale(false);
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

        private void ApplyUiScale(bool resize = true)
        {
            if (_scaleLayout is null || _applyingScale || IsDisposed)
                return;

            _applyingScale = true;
            Action restoreEditing = _scaleLayout.PauseEditing();
            SuspendLayout();
            try
            {
                float ratio = _scaleState.Factor / _appliedFactor;
                Size targetSize = new Size((int)Math.Round(ClientSize.Width * ratio),
                                           (int)Math.Round((ClientSize.Height - ScaleControlHeight) * ratio)
                                           + ScaleControlHeight
                                          );
                _appliedFactor = _scaleState.Factor;
                Size chrome = Size - ClientSize;
                float factor = UiScale.Factor(this);
                MinimumSize = new Size(Math.Max((int)(310 * DeviceDpi / 96F),
                                               (int)Math.Round(_logicalMinimum.Width * factor)) + chrome.Width,
                                       (int)Math.Round(_logicalMinimum.Height * factor)
                                       + ScaleControlHeight + chrome.Height
                                      );
                if (resize && WindowState == FormWindowState.Normal)
                {
                    ClientSize = targetSize;
                }
                _scaleLayout.Apply();
                WindowContent.PerformLayout();
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
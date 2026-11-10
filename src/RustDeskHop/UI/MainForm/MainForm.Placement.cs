using RustDeskHop.Settings;

namespace RustDeskHop.UI
{
    internal sealed partial class MainForm
    {
        #region Constants and Fields
        private readonly IWindowPlacementStore? _windowPlacementStore;
        private readonly System.Windows.Forms.Timer _placementTimer = new System.Windows.Forms.Timer { Interval = 500 };
        private bool _rememberPlacement;
        private bool _changingWindowMode;
        private SizeF _normalClientSize;
        private bool _wasMaximized;
        private WindowPlacement? _savedPlacement;
        #endregion

        #region Methods
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            WindowPlacement? placement = _windowPlacementStore?.Load();
            if (placement is not null)
            {
                Rectangle work = Screen.FromControl(this).WorkingArea;
                Size frame = Size - ClientSize;
                double dpi = DeviceDpi / 96D;
                int maxWidth = Math.Max(1, work.Width - frame.Width);
                int maxHeight = Math.Max(1, work.Height - frame.Height);
                ClientSize = new Size((int)Math.Clamp(placement.ClientWidth * dpi,
                                                      Math.Min(ClientSize.Width, maxWidth), maxWidth),
                                      (int)Math.Clamp(placement.ClientHeight * dpi,
                                                      Math.Min(ClientSize.Height, maxHeight), maxHeight));
                // The previous monitor may be gone; always open on a usable screen.
                Location = new Point(work.Left + Math.Max(0, (work.Width - Width) / 2),
                                     work.Top + Math.Max(0, (work.Height - Height) / 2));
            }
            CaptureNormalClientSize();
            _wasMaximized = placement?.Maximized == true;
            if (_wasMaximized)
                WindowState = FormWindowState.Maximized;
            _savedPlacement = placement;
            _placementTimer.Tick += (_, _) => PersistWindowPlacement();
            _rememberPlacement = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!_rememberPlacement || _fullscreen || _changingWindowMode || IsApplyingUiScale
                || WindowState == FormWindowState.Minimized)
                return;

            _wasMaximized = WindowState == FormWindowState.Maximized;
            if (!_wasMaximized)
                CaptureNormalClientSize();
            _placementTimer.Stop();
            _placementTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            PersistWindowPlacement();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _placementTimer.Dispose();
            base.Dispose(disposing);
        }

        private void CaptureNormalClientSize()
        {
            float dpi = DeviceDpi / 96F;
            _normalClientSize = new SizeF(ClientSize.Width / dpi, ClientSize.Height / dpi);
        }

        private void PersistWindowPlacement()
        {
            _placementTimer.Stop();
            if (!_rememberPlacement || _windowPlacementStore is null || _changingWindowMode)
                return;

            // Fullscreen and minimized windows retain the last normal size and maximized preference.
            WindowPlacement placement = new WindowPlacement(_normalClientSize.Width,
                                                            _normalClientSize.Height,
                                                            _wasMaximized);
            if (placement == _savedPlacement)
                return;
            _windowPlacementStore.Save(placement);
            _savedPlacement = placement;
        }
        #endregion
    }
}

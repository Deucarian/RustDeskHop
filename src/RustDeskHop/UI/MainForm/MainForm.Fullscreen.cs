namespace RustDeskHop.UI
{
    internal sealed partial class MainForm
    {
        #region Constants and Fields
        private bool _fullscreen;
        private Rectangle _windowBounds;
        private FormWindowState _windowState;
        private Size _windowMinimum;
        #endregion

        #region Methods
        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (keyData == Keys.F11 || (_fullscreen && keyData == Keys.Escape))
            {
                ToggleFullscreen();
                return true;
            }
            return base.ProcessCmdKey(ref message, keyData);
        }

        private void ToggleFullscreen()
        {
            PersistWindowPlacement();
            _changingWindowMode = true;
            try
            {
                ChangeFullscreen();
            }
            finally
            {
                _changingWindowMode = false;
            }
        }

        private void ChangeFullscreen()
        {
            if (!_fullscreen)
            {
                _windowState = WindowState;
                _windowBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                _windowMinimum = MinimumSize;
                Rectangle screenBounds = Screen.FromControl(this).Bounds;
                _fullscreen = true;
                _fullscreenButton.Text = "Exit fullscreen (Esc)";
                WindowState = FormWindowState.Normal;
                MinimumSize = Size.Empty;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = screenBounds;
            }
            else
            {
                _fullscreen = false;
                _fullscreenButton.Text = "Fullscreen (F11)";
                FormBorderStyle = FormBorderStyle.Sizable;
                MinimumSize = _windowMinimum;
                Bounds = _windowBounds;
                WindowState = _windowState;
            }
        }
        #endregion
    }
}

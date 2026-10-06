using RustDeskHop.Branding;

namespace RustDeskHop.UI
{
    internal sealed class TrayController : IDisposable
    {
        #region Constants and Fields
        private readonly Form _window;
        private readonly bool _showNotifications;
        private readonly ToolStripMenuItem _exitItem;
        private FormWindowState _restoreState = FormWindowState.Normal;
        private bool _exiting;
        private bool _disposed;
        private bool _explainedTray;
        private bool _restoring;
        #endregion

        #region Constructors and Destructors
        internal TrayController(Form window, bool showNotifications = true)
        {
            _window = window;
            _showNotifications = showNotifications;
            Menu.Items.Add("&Open RustDeskHop", null, (_, _) => OpenWindow());
            Menu.Items.Add(new ToolStripSeparator());
            _exitItem = (ToolStripMenuItem)Menu.Items.Add("E&xit", null, (_, _) => Exit());
            Menu.Opening += (_, _) => _exitItem.Enabled = window.Enabled;
            NotificationIcon = new NotifyIcon
            {
                Text = "RustDeskHop — click to open",
                Icon = AppBranding.Icon,
                ContextMenuStrip = Menu,
                Visible = true
            };
            NotificationIcon.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    OpenWindow();
            };
            NotificationIcon.BalloonTipClicked += (_, _) => OpenWindow();
            window.Resize += OnResize;
            window.FormClosing += OnFormClosing;
            window.FormClosed += OnFormClosed;
            window.Disposed += OnWindowDisposed;
        }
        #endregion

        #region Properties and Indexers
        internal NotifyIcon NotificationIcon { get; }
        internal ContextMenuStrip Menu { get; } = new ContextMenuStrip();
        #endregion

        #region Methods
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _window.Resize -= OnResize;
            _window.FormClosing -= OnFormClosing;
            _window.FormClosed -= OnFormClosed;
            _window.Disposed -= OnWindowDisposed;
            NotificationIcon.Visible = false;
            NotificationIcon.Dispose();
            Menu.Dispose();

            // AppBranding.Icon is shared by all windows, so it is not ours to dispose.
        }

        internal void OpenWindow()
        {
            if (_disposed || _window.IsDisposed)
                return;

            FormWindowState targetState = _restoreState;
            _restoring = true;
            try
            {
                // Showing a hidden minimized HWND can reapply its cached native
                // state. Restore after Show, without hiding again during Resize.
                _window.Show();
                _window.WindowState = targetState;
            }
            finally
            {
                _restoring = false;
            }

            _window.Activate();

            // Do not strand an open network editor or sign-in dialog behind its owner.
            Form? owned = _window.OwnedForms.LastOrDefault(form => form.Visible && !form.IsDisposed);
            if (owned is not null)
            {
                if (owned.WindowState == FormWindowState.Minimized)
                    owned.WindowState = FormWindowState.Normal;
                owned.Activate();
            }
        }

        internal void Exit()
        {
            if (_disposed)
                return;

            // An editor/confirmation must be finished or cancelled by its user first.
            if (!_window.Enabled)
            {
                OpenWindow();
                return;
            }

            _exiting = true;
            _window.Close();
            if (!_window.IsDisposed)
                _exiting = false; // Respect any other close veto.
        }

        // Never veto logoff, Windows shutdown, Task Manager or Application.Exit().
        internal static bool ShouldHideOnClose(CloseReason reason) => reason == CloseReason.UserClosing;

        private void OnResize(object? sender, EventArgs e)
        {
            if (_restoring)
                return;

            if (_window.WindowState == FormWindowState.Minimized)
                HideWindow();
            else
                _restoreState = _window.WindowState;
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_exiting || !ShouldHideOnClose(e.CloseReason))
                return;

            e.Cancel = true;
            HideWindow();
        }

        private void HideWindow()
        {
            if (_disposed || !_window.Enabled)
                return;

            _window.Hide();
            if (_explainedTray || !_showNotifications)
                return;

            _explainedTray = true;
            NotificationIcon.ShowBalloonTip(4000,
                                            "RustDeskHop is still running",
                                            "Click the tray icon to reopen. Right-click it and choose Exit to quit. RustDesk sessions stay open.",
                                            ToolTipIcon.Info
                                           );
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e) => Dispose();
        private void OnWindowDisposed(object? sender, EventArgs e) => Dispose();
        #endregion
    }
}
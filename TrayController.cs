namespace Simultria.RustDeskCompanion;

internal sealed class TrayController : IDisposable
{
    private readonly Form window;
    private readonly bool showNotifications;
    private readonly ToolStripMenuItem exitItem;
    private FormWindowState restoreState = FormWindowState.Normal;
    private bool exiting;
    private bool disposed;
    private bool explainedTray;
    private bool restoring;

    internal NotifyIcon NotificationIcon { get; }
    internal ContextMenuStrip Menu { get; } = new();

    internal TrayController(Form window, bool showNotifications = true)
    {
        this.window = window;
        this.showNotifications = showNotifications;
        Menu.Items.Add("&Open RustDeskHop", null, (_, _) => OpenWindow());
        Menu.Items.Add(new ToolStripSeparator());
        exitItem = (ToolStripMenuItem)Menu.Items.Add("E&xit", null, (_, _) => Exit());
        Menu.Opening += (_, _) => exitItem.Enabled = window.Enabled;
        NotificationIcon = new NotifyIcon
        {
            Text = "RustDeskHop — click to open",
            Icon = AppBranding.Icon,
            ContextMenuStrip = Menu,
            Visible = true
        };
        NotificationIcon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) OpenWindow(); };
        NotificationIcon.BalloonTipClicked += (_, _) => OpenWindow();
        window.Resize += OnResize;
        window.FormClosing += OnFormClosing;
        window.FormClosed += OnFormClosed;
        window.Disposed += OnWindowDisposed;
    }

    internal void OpenWindow()
    {
        if (disposed || window.IsDisposed) return;
        var targetState = restoreState;
        restoring = true;
        try
        {
            // Showing a hidden minimized HWND can reapply its cached native
            // state. Restore after Show, without hiding again during Resize.
            window.Show();
            window.WindowState = targetState;
        }
        finally { restoring = false; }
        window.Activate();
        // Do not strand an open network editor or sign-in dialog behind its owner.
        var owned = window.OwnedForms.LastOrDefault(form => form.Visible && !form.IsDisposed);
        if (owned is not null)
        {
            if (owned.WindowState == FormWindowState.Minimized) owned.WindowState = FormWindowState.Normal;
            owned.Activate();
        }
    }

    internal void Exit()
    {
        if (disposed) return;
        // An editor/confirmation must be finished or cancelled by its user first.
        if (!window.Enabled) { OpenWindow(); return; }
        exiting = true;
        window.Close();
        if (!window.IsDisposed) exiting = false; // Respect any other close veto.
    }

    private void OnResize(object? sender, EventArgs e)
    {
        if (restoring) return;
        if (window.WindowState == FormWindowState.Minimized) HideWindow();
        else restoreState = window.WindowState;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (exiting || !ShouldHideOnClose(e.CloseReason)) return;
        e.Cancel = true;
        HideWindow();
    }

    // Never veto logoff, Windows shutdown, Task Manager or Application.Exit().
    internal static bool ShouldHideOnClose(CloseReason reason) => reason == CloseReason.UserClosing;

    private void HideWindow()
    {
        if (disposed || !window.Enabled) return;
        window.Hide();
        if (explainedTray || !showNotifications) return;
        explainedTray = true;
        NotificationIcon.ShowBalloonTip(4000, "RustDeskHop is still running",
            "Click the tray icon to reopen. Right-click it and choose Exit to quit. RustDesk sessions stay open.",
            ToolTipIcon.Info);
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e) => Dispose();
    private void OnWindowDisposed(object? sender, EventArgs e) => Dispose();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        window.Resize -= OnResize;
        window.FormClosing -= OnFormClosing;
        window.FormClosed -= OnFormClosed;
        window.Disposed -= OnWindowDisposed;
        NotificationIcon.Visible = false;
        NotificationIcon.Dispose();
        Menu.Dispose();
        // AppBranding.Icon is shared by all windows, so it is not ours to dispose.
    }
}

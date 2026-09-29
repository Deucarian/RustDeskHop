using Microsoft.VisualBasic.ApplicationServices;

namespace Simultria.RustDeskCompanion;

// Use the WinForms framework's per-user single-instance activation/IPC. The
// elevated public-profile command is handled before this host in Program.Main.
internal sealed class RustDeskHopApplication : WindowsFormsApplicationBase
{
    private TrayController? tray;

    internal RustDeskHopApplication()
    {
        IsSingleInstance = true;
        EnableVisualStyles = true;
        HighDpiMode = Application.HighDpiMode;
        ShutdownStyle = ShutdownMode.AfterMainFormCloses;
    }

    protected override void OnCreateMainForm()
    {
        MainForm = new MainForm();
        tray = new TrayController(MainForm);
    }

    protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
    {
        // Activate also restores hidden windows and gives any modal editor focus.
        eventArgs.BringToForeground = false;
        base.OnStartupNextInstance(eventArgs);
        tray?.OpenWindow();
    }

    protected override void OnShutdown()
    {
        tray?.Dispose();
        base.OnShutdown();
    }
}

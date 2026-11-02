using Microsoft.VisualBasic.ApplicationServices;
using RustDeskHop.UI;

namespace RustDeskHop
{
    // Use the WinForms framework's per-user single-instance activation/IPC. The
    // elevated public-profile command is handled before this host in Program.Main.
    internal sealed class RustDeskHopApplication : WindowsFormsApplicationBase
    {
        #region Constants and Fields
        private TrayController? _tray;
        private readonly Func<Form> _createMainForm;
        #endregion

        #region Constructors and Destructors
        internal RustDeskHopApplication(Func<Form> createMainForm)
        {
            _createMainForm = createMainForm;
            IsSingleInstance = true;
            EnableVisualStyles = true;
            HighDpiMode = Application.HighDpiMode;
            ShutdownStyle = ShutdownMode.AfterMainFormCloses;
        }
        #endregion

        #region Methods
        protected override void OnCreateMainForm()
        {
            MainForm = _createMainForm();
            _tray = new TrayController(MainForm);
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            // Activate also restores hidden windows and gives any modal editor focus.
            eventArgs.BringToForeground = false;
            base.OnStartupNextInstance(eventArgs);
            _tray?.OpenWindow();
        }

        protected override void OnShutdown()
        {
            _tray?.Dispose();
            base.OnShutdown();
        }
        #endregion
    }
}
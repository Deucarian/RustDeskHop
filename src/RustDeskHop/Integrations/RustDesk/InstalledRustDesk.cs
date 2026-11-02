using RustDeskHop.Connections;
using RustDeskHop.Models;

namespace RustDeskHop.Integrations.RustDesk
{
    // Windows adapter. Parsing and launch helpers remain independently testable.
    internal sealed class InstalledRustDesk : IRustDeskClient, IRustDeskState
    {
        #region Methods
        public string? FindExecutable() => RustDeskLocator.Find();
        public RustDeskDefaultRoute ReadDefaultRoute() => RustDeskConfigReader.ReadDefaultRoute();

        public ServerProfile? DetectDefaultProfile(AppSettings settings) =>
            RustDeskConfigReader.DetectDefaultProfile(settings);

        public bool HasLoginToken() => RustDeskAccountState.HasLoginToken();
        public string? ReadLoginFingerprint() => RustDeskAccountState.ReadLoginFingerprint();
        public void Open(string executablePath) => RustDeskLauncher.Open(executablePath);
        public void Connect(string executablePath, string target) => RustDeskLauncher.Connect(executablePath, target);
        #endregion
    }
}
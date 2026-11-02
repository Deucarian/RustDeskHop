using System.Diagnostics;

namespace RustDeskHop.Integrations.RustDesk
{
    internal static class RustDeskLauncher
    {
        #region Methods
        public static void Open(string rustDeskPath)
        {
            Process.Start(new ProcessStartInfo(rustDeskPath) { UseShellExecute = true, });
        }

        public static void Connect(string rustDeskPath, string connectionTarget)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(rustDeskPath)
            {
                UseShellExecute = true,
            };
            startInfo.ArgumentList.Add("--connect");
            startInfo.ArgumentList.Add(connectionTarget);
            Process.Start(startInfo);
        }
        #endregion
    }
}
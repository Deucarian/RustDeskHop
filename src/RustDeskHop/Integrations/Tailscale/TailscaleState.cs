using System.Diagnostics;

namespace RustDeskHop.Integrations.Tailscale
{
    internal static class TailscaleState
    {
        #region Methods
        public static bool IsRunning()
        {
            List<Process> processes = Process
                .GetProcessesByName("tailscaled")
                .Concat(Process.GetProcessesByName("tailscale-ipn"))
                .ToList();
            try
            {
                return processes.Count > 0;
            }
            finally
            {
                processes.ForEach(process => process.Dispose());
            }
        }

        public static bool TryStart()
        {
            string[] candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                             "Tailscale",
                             "tailscale-ipn.exe"
                            ),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                             "Tailscale",
                             "tailscale-ipn.exe"
                            ),
            };
            string? path = candidates.FirstOrDefault(File.Exists);
            if (path is null)
            {
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
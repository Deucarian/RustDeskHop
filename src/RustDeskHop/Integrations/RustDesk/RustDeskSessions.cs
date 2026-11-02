using System.Diagnostics;

namespace RustDeskHop.Integrations.RustDesk
{
    internal static class RustDeskSessions
    {
        #region Methods
        public static bool HasVisibleWindows()
        {
            return Process
                .GetProcessesByName("rustdesk")
                .Any(p =>
                     {
                         try
                         {
                             return p.MainWindowHandle != IntPtr.Zero;
                         }
                         catch
                         {
                             return false;
                         }
                     }
                    );
        }

        public static void CloseVisibleWindows()
        {
            foreach (Process? process in Process.GetProcessesByName("rustdesk"))
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        process.CloseMainWindow();
                        process.WaitForExit(1500);
                    }
                }
                catch
                {
                    // Never force-kill RustDesk's service. It may be providing unattended access.
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        #endregion
    }
}
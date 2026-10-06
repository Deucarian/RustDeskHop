using RustDeskHop.Models;
using RustDeskHop.Integrations.RustDesk;

namespace RustDeskHop
{
    internal static class Program
    {
        #region Methods
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--verify-install")
            {
                // Smoke-test the actual distributed executable without the single-instance
                // host, user settings, RustDesk, startup registration or a remote connection.
                try
                {
                    ApplicationConfiguration.Initialize();
                    using UI.MainForm preview = ApplicationComposition.CreateMainForm(new AppSettings());
                    _ = preview.Handle;
                    preview.PerformLayout();
                    Environment.ExitCode = preview.Icon is not null && preview.Controls.Count > 0 ? 0 : 1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    Environment.ExitCode = 1;
                }

                return;
            }

            if (RustDeskPublicProfileSetup.TryHandleCommand(args, out int exitCode))
            {
                Environment.ExitCode = exitCode;
                return;
            }

            ApplicationConfiguration.Initialize();
            new RustDeskHopApplication(() => ApplicationComposition.CreateMainForm()).Run(args);
        }
        #endregion
    }
}
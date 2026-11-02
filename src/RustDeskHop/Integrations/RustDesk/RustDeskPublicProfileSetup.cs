using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using RustDeskHop.Connections;

namespace RustDeskHop.Integrations.RustDesk
{
    internal static class RustDeskPublicProfileSetup
    {
        #region Constants and Fields
        public const string COMMAND_NAME = "--prepare-public-login";
        #endregion

        #region Methods
        public static bool TryHandleCommand(string[] args, out int exitCode)
        {
            if (args.Length == 0 || !string.Equals(args[0], COMMAND_NAME, StringComparison.OrdinalIgnoreCase))
            {
                exitCode = 0;
                return false;
            }

            // UAC can start the helper as a different administrator. Never silently edit
            // that account's RustDesk settings instead of the requesting user's settings.
            PublicSetupResult result = args.Length == 2
                ? ApplyElevated(args[1])
                : new PublicSetupResult(false,
                                        "Start public sign-in preparation from RustDeskHop, not from the command line."
                                       );
            if (!result.Success)
            {
                MessageBox.Show(result.Message,
                                "RustDeskHop public setup failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                               );
            }

            exitCode = result.Success ? 0 : 1;
            return true;
        }

        public static async Task<PublicSetupResult> RequestAsync()
        {
            string? executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                return new PublicSetupResult(false, "RustDeskHop could not locate its own executable.");
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(executablePath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                };
                startInfo.ArgumentList.Add(COMMAND_NAME);
                startInfo.ArgumentList.Add(WindowsIdentity.GetCurrent().User?.Value
                                           ?? throw new
                                               InvalidOperationException("Windows could not identify the requesting account."
                                                                        )
                                          );
                using Process? process = Process.Start(startInfo);
                if (process is null)
                {
                    return new PublicSetupResult(false, "Windows did not start the one-time setup helper.");
                }

                await process.WaitForExitAsync();
                return process.ExitCode == 0
                    ? new PublicSetupResult(true, "RustDesk is ready to show its public account sign-in.")
                    : new PublicSetupResult(false,
                                            "The one-time RustDesk public setup did not complete. Any detailed error was shown by the administrator helper."
                                           );
            }
            catch (Win32Exception ex)when (ex.NativeErrorCode == 1223)
            {
                return new PublicSetupResult(false, "The Windows administrator prompt was cancelled.");
            }
            catch (Exception ex)
            {
                return new PublicSetupResult(false, $"The one-time setup could not start: {ex.Message}");
            }
        }

        internal static bool IsSameWindowsUser(string? requestingSid, string? elevatedSid) =>
            !string.IsNullOrWhiteSpace(requestingSid)
            && !string.IsNullOrWhiteSpace(elevatedSid)
            && string.Equals(requestingSid, elevatedSid, StringComparison.Ordinal);

        private static PublicSetupResult ApplyElevated(string requestingSid)
        {
            if (!IsAdministrator())
            {
                return new PublicSetupResult(false,
                                             "Administrator approval is required for installed RustDesk configuration."
                                            );
            }

            if (!IsSameWindowsUser(requestingSid, WindowsIdentity.GetCurrent().User?.Value))
            {
                return new PublicSetupResult(false,
                                             "Windows started setup as a different administrator. No RustDesk settings were changed. "
                                             + "Ask your administrator to configure RustDesk's public service, then sign in to RustDesk from your own Windows account and retry. "
                                             + "Normal RustDeskHop connections do not require administrator rights."
                                            );
            }

            string? rustDeskPath = RustDeskLocator.Find();
            if (rustDeskPath is null)
            {
                return new PublicSetupResult(false, "RustDesk was not found in the usual installation locations.");
            }

            return PublicProfileTransaction.Prepare(RustDeskPaths.UserConfigPath,
                                                    RustDeskPaths.ServiceConfigPath,
                                                    (name, value) => RunRustDeskOption(rustDeskPath, name, value)
                                                   );
        }

        private static void RunRustDeskOption(string rustDeskPath, string name, string value)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(rustDeskPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("--option");
            startInfo.ArgumentList.Add(name);
            startInfo.ArgumentList.Add(value);
            using Process process = Process.Start(startInfo)
                                    ?? throw new
                                        InvalidOperationException("RustDesk did not start its configuration command.");
            if (!process.WaitForExit(10_000))
            {
                try
                {
                    process.Kill();
                }
                catch { }

                throw new TimeoutException($"RustDesk did not finish updating {name}.");
            }

            if (process.ExitCode != 0)
            {
                throw new
                    InvalidOperationException($"RustDesk rejected the {name} update (exit code {process.ExitCode}).");
            }
        }

        private static bool IsAdministrator()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        #endregion
    }
}
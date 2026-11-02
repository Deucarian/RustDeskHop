using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using RustDeskHop.Models;
using RustDeskHop.Connections;

namespace RustDeskHop.Integrations.RustDesk
{
    internal static class RustDeskConfigReader
    {
        #region Methods
        public static string? ReadConfiguredServer()
        {
            return ReadConfiguredServer(RustDeskPaths.UserConfigPath);
        }

        public static RustDeskDefaultRoute ReadDefaultRoute(string? path = null)
        {
            return TryReadConfiguredServer(path ?? RustDeskPaths.UserConfigPath, out string? server)
                ? ClassifyServer(server)
                : RustDeskDefaultRoute.UNKNOWN;
        }

        public static ServerProfile? DetectDefaultProfile(AppSettings settings, string? path = null)
        {
            if (!TryReadConfiguredServer(path ?? RustDeskPaths.UserConfigPath, out string? configured))
                return null;

            RustDeskDefaultRoute route = ClassifyServer(configured);
            if (route == RustDeskDefaultRoute.UNKNOWN)
                return null;

            if (route == RustDeskDefaultRoute.PUBLIC)
            {
                return settings.Profiles.FirstOrDefault(p => p.IsPublic);
            }

            if (!TryGetHost(configured!, out string? configuredHost))
                return null;

            return settings.Profiles.FirstOrDefault(p =>
                                                        string.Equals(p.ServerAddress,
                                                                      configured,
                                                                      StringComparison.OrdinalIgnoreCase
                                                                     )
                                                        || (TryGetHost(p.ServerAddress, out string? host)
                                                            && string.Equals(host,
                                                                             configuredHost,
                                                                             StringComparison.OrdinalIgnoreCase
                                                                            ))
                                                   );
        }

        internal static string? ReadConfiguredServer(string path) =>
            TryReadConfiguredServer(path, out string? server) ? server : null;

        private static bool TryReadConfiguredServer(string path, out string? server)
        {
            server = null;
            try
            {
                string text = File.ReadAllText(path);

                // The top-level value is the effective server in current RustDesk builds.
                // Prefer it over a stale custom-rendezvous-server option when both exist.
                string? rendezvous = ReadServerSetting(text, "rendezvous_server");
                string? custom = ReadServerSetting(text, "custom-rendezvous-server");
                if (!string.IsNullOrWhiteSpace(rendezvous))
                {
                    server = rendezvous.Trim();
                }
                else
                {
                    server = custom?.Trim();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? ReadServerSetting(string text, string name)
        {
            MatchCollection lines = Regex.Matches(text,
                                                  $"^[ \\t]*{Regex.Escape(name)}[ \\t]*=.*$",
                                                  RegexOptions.Multiline | RegexOptions.IgnoreCase
                                                 );
            if (lines.Count == 0)
                return null;

            if (lines.Count != 1)
                throw new FormatException("Duplicate RustDesk server setting.");

            Match value = Regex.Match(lines[0].Value,
                                      "=[ \\t]*(['\"])(?<value>[^\\r\\n]*?)\\1[ \\t]*(?:#.*)?\\r?$",
                                      RegexOptions.CultureInvariant
                                     );
            if (!value.Success)
                throw new FormatException("Unreadable RustDesk server setting.");

            return value.Groups["value"].Value;
        }

        private static RustDeskDefaultRoute ClassifyServer(string? server)
        {
            if (string.IsNullOrWhiteSpace(server)
                || string.Equals(server, "public", StringComparison.OrdinalIgnoreCase))
            {
                return RustDeskDefaultRoute.PUBLIC;
            }

            // Match RustDesk's public rendezvous hosts, not any private host named rs-*.
            if (!TryGetHost(server, out string? host))
                return RustDeskDefaultRoute.UNKNOWN;

            return Regex.IsMatch(host,
                                 @"^rs-[a-z0-9-]+\.rustdesk\.com$",
                                 RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                                )
                ? RustDeskDefaultRoute.PUBLIC
                : RustDeskDefaultRoute.PRIVATE;
        }

        private static bool TryGetHost(string address, [NotNullWhen(true)] out string? host)
        {
            try
            {
                host = ServerAddressParser.GetHost(address.Trim().TrimEnd('/'));
                return true;
            }
            catch (FormatException)
            {
                host = null;
                return false;
            }
        }
        #endregion
    }
}
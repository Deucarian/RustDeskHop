using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RustDeskHop.Integrations.RustDesk
{
    internal static class RustDeskAccountState
    {
        #region Constants and Fields
        private static readonly Regex _accessTokenPattern =
            new Regex("^\\s*access_token\\s*=\\s*(['\"])(?<value>.*?)\\1",
                      RegexOptions.Multiline | RegexOptions.CultureInvariant
                     );
        #endregion

        #region Methods
        public static bool HasLoginToken(string? path = null) => ReadLoginFingerprint(path) is not null;

        internal static string? ReadLoginFingerprint(string? path = null)
        {
            path ??= RustDeskPaths.LocalConfigPath;
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                Match match = _accessTokenPattern.Match(File.ReadAllText(path));
                string token = match.Groups["value"].Value;
                return match.Success && !string.IsNullOrWhiteSpace(token)
                    ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
                    : null;
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}
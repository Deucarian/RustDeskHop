using System.Text.RegularExpressions;

namespace RustDeskHop.Integrations.RustDesk
{
    internal static class RustDeskTomlEditor
    {
        #region Constants and Fields
        private static readonly Regex _serverSettingLine =
            new
                Regex("^[ \\t]*(?:rendezvous_server|custom-rendezvous-server|relay-server|api-server|key)[ \\t]*=.*(?:\\r?\\n|$)",
                      RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                     );
        #endregion

        #region Methods
        public static string ClearCustomServer(string contents)
        {
            return _serverSettingLine.Replace(contents, "");
        }

        public static string? ReadSetting(string contents, string name)
        {
            string pattern = $"^\\s*{Regex.Escape(name)}\\s*=\\s*(['\"])(?<value>.*?)\\1";
            Match match = Regex.Match(contents,
                                      pattern,
                                      RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                                     );
            return match.Success ? match.Groups["value"].Value : null;
        }
        #endregion
    }
}
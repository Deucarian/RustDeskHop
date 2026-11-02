namespace RustDeskHop.Integrations.RustDesk
{
    internal static class RustDeskPaths
    {
        #region Properties and Indexers
        public static string UserConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "RustDesk",
                         "config",
                         "RustDesk2.toml"
                        );

        public static string LocalConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "RustDesk",
                         "config",
                         "RustDesk_local.toml"
                        );

        public static string ServiceConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                         "ServiceProfiles",
                         "LocalService",
                         "AppData",
                         "Roaming",
                         "RustDesk",
                         "config",
                         "RustDesk2.toml"
                        );
        #endregion
    }
}
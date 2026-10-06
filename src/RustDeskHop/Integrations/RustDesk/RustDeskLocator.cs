namespace RustDeskHop.Integrations.RustDesk
{
    internal static class RustDeskLocator
    {
        #region Methods
        public static string? Find()
        {
            string[] candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                             "RustDesk",
                             "rustdesk.exe"
                            ),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                             "RustDesk",
                             "rustdesk.exe"
                            ),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                             "RustDesk",
                             "rustdesk.exe"
                            ),
            };
            return candidates.FirstOrDefault(File.Exists);
        }
        #endregion
    }
}
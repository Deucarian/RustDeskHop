using RustDeskHop.Models;
using System.Text.Json;

namespace RustDeskHop.Settings
{
    internal static class ConfigStore
    {
        #region Constants and Fields
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };
        #endregion

        #region Properties and Indexers
        public static string DirectoryPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData
                                                  ), // Persisted location is a compatibility contract, not the code namespace.
                         "SimultriaRustDeskCompanion"
                        );

        public static string FilePath => Path.Combine(DirectoryPath, "settings.json");
        #endregion

        #region Methods
        public static AppSettings Load() => Load(out _);

        public static AppSettings Load(out string? warning) =>
            Load(FilePath, Path.Combine(AppContext.BaseDirectory, "settings.local.json"), out warning);

        public static void Save(AppSettings settings) => Save(settings, FilePath);

        internal static AppSettings Load(string filePath, string? localSeedPath, out string? warning)
        {
            warning = null;
            foreach (string? path in new[]
                     {
                         filePath,
                         localSeedPath
                     })
            {
                if (path is null)
                    continue;

                try
                {
                    AppSettings? saved = Read(path);
                    if (saved is not null)
                        return saved;
                }
                catch (Exception ex)when (IsReadFailure(ex))
                {
                    // Do not replace a damaged file, or import a developer seed instead of
                    // the user's saved computers. Defaults are in memory only.
                    warning = $"Settings could not be loaded from:\n{path}\n\n{ex.Message}\n\n"
                              + "The original file has been left unchanged. Temporary default profiles are shown. "
                              + "Before you save changes, any damaged settings file must be backed up successfully.";
                    return CreateDefaults();
                }
            }

            return CreateDefaults();
        }

        internal static void Validate(AppSettings? settings)
        {
            if (settings?.Profiles is null || settings.Targets is null || settings.Profiles.Count == 0)
                throw new InvalidDataException("Settings must contain a network-profile list and a computer list.");

            HashSet<string> profileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ServerProfile? profile in settings.Profiles)
            {
                if (profile is null
                    || string.IsNullOrWhiteSpace(profile.Id)
                    || string.IsNullOrWhiteSpace(profile.Name)
                    || string.IsNullOrWhiteSpace(profile.ServerAddress)
                    || profile.PublicKey is null
                    || profile.ProbeHost is null
                    || profile.ProbePort is < 1 or > 65535
                    || !profileIds.Add(profile.Id))
                    throw new InvalidDataException("A network profile is incomplete, invalid, or has a duplicate ID.");
            }

            foreach (TargetDefinition? target in settings.Targets)
            {
                if (target is null
                    || string.IsNullOrWhiteSpace(target.Name)
                    || string.IsNullOrWhiteSpace(target.RustDeskId)
                    || string.IsNullOrWhiteSpace(target.ProfileId))
                    throw new InvalidDataException("A saved computer is incomplete or invalid.");

                // A deleted profile remains a repairable assignment in the UI, not a reason
                // to discard the user's entire computer list.
            }
        }

        internal static void Save(AppSettings settings, string filePath)
        {
            Validate(settings);
            string directory = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
            Directory.CreateDirectory(directory);
            try
            {
                _ = Read(filePath);
            }
            catch (Exception ex)when (ex is JsonException or InvalidDataException)
            {
                // A readable but damaged file must be preserved before an explicit save.
                // I/O and access failures propagate instead of treating the file as missing.
                string backupDirectory = Path.Combine(directory, "Settings Backups");
                Directory.CreateDirectory(backupDirectory);
                string backupPath = Path.Combine(backupDirectory,
                                                 $"{Path.GetFileName(filePath)}.invalid-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.bak"
                                                );
                File.Copy(filePath, backupPath, false);
            }

            string temporaryPath = filePath + $".{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, _jsonOptions));
                File.Move(temporaryPath, filePath, true);
            }
            finally
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static AppSettings? Read(string path)
        {
            string contents;
            try
            {
                contents = File.ReadAllText(path);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }

            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(contents, _jsonOptions);
            Validate(settings);
            return settings;
        }

        private static bool IsReadFailure(Exception error) =>
            error is JsonException or InvalidDataException or IOException or UnauthorizedAccessException;

        private static AppSettings CreateDefaults() => new AppSettings()
        {
            Profiles =
            [
                new ServerProfile()
                {
                    Id = "public",
                    Name = "RustDesk Public",
                    ServerAddress = "public"
                },
                new ServerProfile()
                {
                    Id = "private-example",
                    Name = "Private RustDesk",
                    ServerAddress = "private-server.example:21116",
                    RequiresPrivateNetwork = true,
                    ProbeHost = "private-server.example",
                    ProbePort = 21116,
                },
            ],
            Targets = [],
        };
        #endregion
    }
}
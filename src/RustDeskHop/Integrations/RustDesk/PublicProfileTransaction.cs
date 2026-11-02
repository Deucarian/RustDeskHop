using RustDeskHop.Connections;

namespace RustDeskHop.Integrations.RustDesk
{
    internal static class PublicProfileTransaction
    {
        #region Methods
        // Explicit paths and a command runner allow tests to exercise the actual transaction
        // without administrator approval, real RustDesk processes, or live configuration.
        internal static PublicSetupResult Prepare(string userConfigPath,
                                                  string serviceConfigPath,
                                                  Action<string, string> runOption)
        {
            string[] optionNames = new[]
            {
                "custom-rendezvous-server",
                "relay-server",
                "api-server",
                "key"
            };
            Dictionary<string, string> backups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> originalOptions =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            List<string> attemptedOptions = new List<string>();
            try
            {
                string timestamp = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}";
                foreach (string? configPath in new[]
                         {
                             userConfigPath,
                             serviceConfigPath
                         })
                {
                    // File.Exists also returns false on access errors. Only genuinely absent
                    // files may be skipped; unreadable state must stop the preflight.
                    try
                    {
                        _ = File.GetAttributes(configPath);
                    }
                    catch (FileNotFoundException)
                    {
                        continue;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        continue;
                    }

                    string scope = string.Equals(configPath, serviceConfigPath, StringComparison.OrdinalIgnoreCase)
                        ? "service"
                        : "user";
                    string backupDirectory =
                        Path.Combine(Path.GetDirectoryName(configPath)
                                     ?? throw new
                                         InvalidOperationException("RustDesk's configuration directory could not be found."
                                                                  ),
                                     "RustDeskHop Backups"
                                    );
                    Directory.CreateDirectory(backupDirectory);
                    string backupPath = Path.Combine(backupDirectory, $"RustDesk2-{scope}-{timestamp}.toml");
                    File.Copy(configPath, backupPath, false);
                    backups[configPath] = backupPath;

                    // Read the captured snapshot, not a live file that may have changed since
                    // the backup. The installed service's snapshot takes precedence if present.
                    string contents = File.ReadAllText(backupPath);
                    foreach (string? option in optionNames)
                    {
                        originalOptions[option] = RustDeskTomlEditor.ReadSetting(contents, option) ?? "";
                    }
                }

                if (backups.Count == 0)
                {
                    return new PublicSetupResult(false,
                                                 "No RustDesk configuration could be backed up. Open RustDesk once, then retry. No server settings were changed."
                                                );
                }

                // RustDesk's supported management interface updates an installed client/service.
                // Clearing these options returns it to the built-in public rendezvous service.
                foreach (string? option in optionNames)
                {
                    // A failing command can still have partially applied. Include it in the
                    // rollback, but never issue rollback commands for untouched options.
                    attemptedOptions.Add(option);
                    runOption(option, "");
                }

                // Clear stale persisted values too. Some RustDesk versions leave the effective
                // top-level rendezvous server behind even after the visible option is changed.
                foreach (string? configPath in backups.Keys)
                {
                    ClearConfigFile(configPath);
                }

                return new PublicSetupResult(true, "RustDesk is ready to show its public account sign-in.");
            }
            catch (Exception ex)
            {
                if (attemptedOptions.Count == 0)
                {
                    return new PublicSetupResult(false,
                                                 $"RustDesk configuration could not be backed up: {ex.Message} No server settings were changed."
                                                );
                }
                bool restored = RestoreOriginalConfiguration(runOption, backups, originalOptions, attemptedOptions);
                string backupLocations = string.Join(", ", backups.Values.Select(Path.GetDirectoryName).Distinct());
                string recoveryMessage = restored
                    ? "The previous RustDesk configuration was restored."
                    : $"Automatic restore was incomplete. Backup files remain in: {backupLocations}";
                return new PublicSetupResult(false, $"RustDesk public setup failed: {ex.Message} {recoveryMessage}");
            }
        }

        private static bool RestoreOriginalConfiguration(Action<string, string> runOption,
                                                         IReadOnlyDictionary<string, string> backups,
                                                         IReadOnlyDictionary<string, string> originalOptions,
                                                         IReadOnlyList<string> attemptedOptions)
        {
            bool restored = true;
            foreach (string? option in attemptedOptions.Reverse())
            {
                try
                {
                    runOption(option, originalOptions[option]);
                }
                catch
                {
                    restored = false;
                }
            }

            // Management commands can rewrite both files. Restore their distinct snapshots
            // last, so restoring service options cannot overwrite the user's original values.
            foreach (KeyValuePair<string, string> backup in backups)
            {
                try
                {
                    File.Copy(backup.Value, backup.Key, true);
                }
                catch
                {
                    restored = false;
                }
            }

            return restored;
        }

        private static void ClearConfigFile(string path)
        {
            string original = File.ReadAllText(path);
            string updated = RustDeskTomlEditor.ClearCustomServer(original);
            if (string.Equals(original, updated, StringComparison.Ordinal))
            {
                return;
            }

            string temporaryPath = path + ".rustdeskhop.tmp";
            try
            {
                File.WriteAllText(temporaryPath, updated);
                File.Move(temporaryPath, path, true);
            }
            finally
            {
                TryDelete(temporaryPath);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Cleanup failure should not hide the actual setup result.
            }
        }
        #endregion
    }
}
using System.Text;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class ConfigStoreTests
    {
        #region Constants and Fields
        private const string VALID_JSON = """
                                          {"Profiles":[{"Id":"public","Name":"Public","ServerAddress":"public"}],"Targets":[]}
                                          """;
        #endregion

        #region Test Methods
        [Fact]
        public void FirstLoadUsesGenericDefaultsWithoutWritingAFile()
        {
            using TestDirectory temp = new TestDirectory();
            AppSettings settings = ConfigStore.Load(temp.FilePath("settings.json"), null, out string? warning);
            Assert.Null(warning);
            Assert.Empty(settings.Targets);
            Assert.Contains(settings.Profiles, p => p.IsPublic);
            Assert.Empty(Directory.GetFileSystemEntries(temp.Root));
        }

        [Fact]
        public void ValidSettingsRoundTripUnicodeAndMappings()
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("settings.json");
            AppSettings settings = ConfigStore.Load(path, null, out _);
            settings.Targets.Add(new TargetDefinition()
                                     { Name = "测试 / Café 🐇", RustDeskId = "123456789", ProfileId = "public" }
                                );
            ConfigStore.Save(settings, path);
            byte[] original = File.ReadAllBytes(path);
            AppSettings loaded = ConfigStore.Load(path, null, out string? warning);
            Assert.Null(warning);
            Assert.Equal("测试 / Café 🐇", Assert.Single(loaded.Targets).Name);
            Assert.Equal("public", loaded.Targets[0].ProfileId);
            Assert.Equal(original, File.ReadAllBytes(path));
            Assert.Empty(Directory.GetFiles(temp.Root, "*.tmp"));
        }

        [Theory]
        [InlineData("{broken-json")]
        [InlineData("null")]
        [InlineData("{}")]
        [InlineData("{\"Profiles\":null,\"Targets\":[]}")]
        [InlineData("{\"Profiles\":[null],\"Targets\":[]}")]
        [InlineData("{\"Profiles\":[{\"Id\":\"p\",\"ServerAddress\":\"public\"}],\"Targets\":null}")]
        [InlineData("{\"Profiles\":[{\"Id\":\"p\",\"ServerAddress\":\"public\"}],\"Targets\":[null]}")]
        [InlineData("{\"Profiles\":[{\"ServerAddress\":null}],\"Targets\":[]}")]
        [InlineData("{\"Profiles\":[{\"ServerAddress\":\"public\",\"PublicKey\":null}],\"Targets\":[]}")]
        [InlineData("{\"Profiles\":[{\"ServerAddress\":\"public\",\"ProbeHost\":null}],\"Targets\":[]}")]
        [InlineData("{\"Profiles\":[{\"ServerAddress\":\"public\",\"ProbePort\":65536}],\"Targets\":[]}")]
        [InlineData("{\"Profiles\":[{\"Id\":\"p\",\"ServerAddress\":\"public\"},{\"Id\":\"P\",\"ServerAddress\":\"other.example\"}],\"Targets\":[]}"
                   )]
        [InlineData("{\"Profiles\":[{\"Id\":\"p\",\"ServerAddress\":\"public\"}],\"Targets\":[{\"RustDeskId\":null,\"ProfileId\":\"p\"}]}"
                   )]
        public void InvalidSettingsReturnSafeDefaultsAndLeaveOriginalUntouched(string json)
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("settings.json");
            File.WriteAllText(path, json);
            AppSettings loaded = ConfigStore.Load(path, null, out string? warning);
            Assert.NotNull(warning);
            Assert.Contains(path, warning);
            Assert.Contains("unchanged", warning);
            ConfigStore.Validate(loaded);
            Assert.Empty(loaded.Targets);
            Assert.Equal(json, File.ReadAllText(path));
            Assert.Single(Directory.GetFileSystemEntries(temp.Root));
        }

        [Theory]
        [InlineData("{broken-json")]
        [InlineData("{\"Profiles\":null,\"Targets\":[]}")]
        public void ExplicitSavePreservesDamagedFileByteForByteFirst(string damaged)
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("settings.json");
            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(damaged)).ToArray();
            File.WriteAllBytes(path, bytes);
            AppSettings settings = ConfigStore.Load(path, null, out string? warning);
            Assert.NotNull(warning);
            ConfigStore.Save(settings, path);
            string backup = Assert.Single(Directory.GetFiles(temp.FilePath("Settings Backups")));
            Assert.Equal(bytes, File.ReadAllBytes(backup));
            ConfigStore.Load(path, null, out warning);
            Assert.Null(warning);
            Assert.Empty(Directory.GetFiles(temp.Root, "*.tmp"));
        }

        [Fact]
        public void BackupFailurePreventsOverwritingDamagedSettings()
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("settings.json");
            const string DAMAGED = "{recover-this";
            File.WriteAllText(path, DAMAGED);
            File.WriteAllText(temp.FilePath("Settings Backups"), "Blocks creation of the backup directory");
            AppSettings settings = ConfigStore.Load(path, null, out _);
            Assert.ThrowsAny<IOException>(() => ConfigStore.Save(settings, path));
            Assert.Equal(DAMAGED, File.ReadAllText(path));
            Assert.Empty(Directory.GetFiles(temp.Root, "*.tmp"));
        }

        [Fact]
        public void LockedSettingsAreNotTreatedAsMissingOrOverwritten()
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("settings.json");
            File.WriteAllText(path, VALID_JSON);
            using (FileStream locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                AppSettings fallback = ConfigStore.Load(path, null, out string? warning);
                Assert.NotNull(warning);
                Assert.ThrowsAny<IOException>(() => ConfigStore.Save(fallback, path));
            }

            Assert.Equal(VALID_JSON, File.ReadAllText(path));
            Assert.Empty(Directory.GetFiles(temp.Root, "*.tmp"));
        }

        [Fact]
        public void FailedAtomicReplacementLeavesPreviousSettingsAndCleansTemporaryFile()
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("settings.json");
            File.WriteAllText(path, VALID_JSON);
            AppSettings changed = ConfigStore.Load(path, null, out _);
            changed.Profiles[0].Name = "Unsaved change";
            using (FileStream locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Exception error = Record.Exception(() => ConfigStore.Save(changed, path));
                Assert.True(error is IOException or UnauthorizedAccessException,
                            $"Expected a sharing/access failure, got {error}"
                           );
            }

            Assert.Equal(VALID_JSON, File.ReadAllText(path));
            Assert.Empty(Directory.GetFiles(temp.Root, "*.tmp"));
        }

        [Fact]
        public void InvalidDataIsRejectedBeforeWriting()
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("settings.json");
            File.WriteAllText(path, VALID_JSON);
            Assert.Throws<InvalidDataException>(() => ConfigStore.Save(new AppSettings { Profiles = null! }, path));
            Assert.Equal(VALID_JSON, File.ReadAllText(path));
        }

        [Fact]
        public void ADeveloperSeedIsUsedOnlyWhenUserSettingsAreAbsent()
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("settings.json");
            string seed = temp.FilePath("seed.json");
            File.WriteAllText(seed, VALID_JSON);
            Assert.Single(ConfigStore.Load(path, seed, out string? warning).Profiles);
            Assert.Null(warning);
            Assert.False(File.Exists(path));
            File.WriteAllText(path, "{broken");
            AppSettings fallback = ConfigStore.Load(path, seed, out warning);
            Assert.NotNull(warning);
            Assert.Equal(2, fallback.Profiles.Count);
            Assert.Equal("{broken", File.ReadAllText(path));
            Assert.Equal(VALID_JSON, File.ReadAllText(seed));
        }

        [Fact]
        public void ComputersWithDeletedProfilesRemainAvailableForRepair()
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("settings.json");
            AppSettings settings = ConfigStore.Load(path, null, out _);
            settings.Targets.Add(new TargetDefinition()
                                     { Name = "Keep me", RustDeskId = "123456789", ProfileId = "deleted" }
                                );
            ConfigStore.Save(settings, path);
            Assert.Equal("deleted", Assert.Single(ConfigStore.Load(path, null, out string? warning).Targets).ProfileId);
            Assert.Null(warning);
        }
        #endregion
    }
}
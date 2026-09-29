using System.Text;
using Simultria.RustDeskCompanion;
using Xunit;

namespace RustDeskHop.Tests;

public sealed class ConfigStoreTests
{
    private const string ValidJson = """
        {"Profiles":[{"Id":"public","Name":"Public","ServerAddress":"public"}],"Targets":[]}
        """;

    [Fact]
    public void FirstLoadUsesGenericDefaultsWithoutWritingAFile()
    {
        using var temp = new TestDirectory();
        var settings = ConfigStore.Load(temp.FilePath("settings.json"), null, out var warning);
        Assert.Null(warning);
        Assert.Empty(settings.Targets);
        Assert.Contains(settings.Profiles, p => p.IsPublic);
        Assert.Empty(Directory.GetFileSystemEntries(temp.Root));
    }

    [Fact]
    public void ValidSettingsRoundTripUnicodeAndMappings()
    {
        using var temp = new TestDirectory();
        var path = temp.FilePath("settings.json");
        var settings = ConfigStore.Load(path, null, out _);
        settings.Targets.Add(new() { Name = "测试 / Café 🐇", RustDeskId = "123456789", ProfileId = "public" });
        ConfigStore.Save(settings, path);
        var original = File.ReadAllBytes(path);
        var loaded = ConfigStore.Load(path, null, out var warning);
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
    [InlineData("{\"Profiles\":[{\"Id\":\"p\",\"ServerAddress\":\"public\"},{\"Id\":\"P\",\"ServerAddress\":\"other.example\"}],\"Targets\":[]}")]
    [InlineData("{\"Profiles\":[{\"Id\":\"p\",\"ServerAddress\":\"public\"}],\"Targets\":[{\"RustDeskId\":null,\"ProfileId\":\"p\"}]}")]
    public void InvalidSettingsReturnSafeDefaultsAndLeaveOriginalUntouched(string json)
    {
        using var temp = new TestDirectory();
        var path = temp.FilePath("settings.json");
        File.WriteAllText(path, json);
        var loaded = ConfigStore.Load(path, null, out var warning);
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
        using var temp = new TestDirectory();
        var path = temp.FilePath("settings.json");
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(damaged)).ToArray();
        File.WriteAllBytes(path, bytes);
        var settings = ConfigStore.Load(path, null, out var warning);
        Assert.NotNull(warning);
        ConfigStore.Save(settings, path);
        var backup = Assert.Single(Directory.GetFiles(temp.FilePath("Settings Backups")));
        Assert.Equal(bytes, File.ReadAllBytes(backup));
        ConfigStore.Load(path, null, out warning);
        Assert.Null(warning);
        Assert.Empty(Directory.GetFiles(temp.Root, "*.tmp"));
    }

    [Fact]
    public void BackupFailurePreventsOverwritingDamagedSettings()
    {
        using var temp = new TestDirectory();
        var path = temp.FilePath("settings.json");
        const string damaged = "{recover-this";
        File.WriteAllText(path, damaged);
        File.WriteAllText(temp.FilePath("Settings Backups"), "Blocks creation of the backup directory");
        var settings = ConfigStore.Load(path, null, out _);
        Assert.ThrowsAny<IOException>(() => ConfigStore.Save(settings, path));
        Assert.Equal(damaged, File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(temp.Root, "*.tmp"));
    }

    [Fact]
    public void LockedSettingsAreNotTreatedAsMissingOrOverwritten()
    {
        using var temp = new TestDirectory();
        var path = temp.FilePath("settings.json");
        File.WriteAllText(path, ValidJson);
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var fallback = ConfigStore.Load(path, null, out var warning);
            Assert.NotNull(warning);
            Assert.ThrowsAny<IOException>(() => ConfigStore.Save(fallback, path));
        }
        Assert.Equal(ValidJson, File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(temp.Root, "*.tmp"));
    }

    [Fact]
    public void FailedAtomicReplacementLeavesPreviousSettingsAndCleansTemporaryFile()
    {
        using var temp = new TestDirectory();
        var path = temp.FilePath("settings.json");
        File.WriteAllText(path, ValidJson);
        var changed = ConfigStore.Load(path, null, out _);
        changed.Profiles[0].Name = "Unsaved change";
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var error = Record.Exception(() => ConfigStore.Save(changed, path));
            Assert.True(error is IOException or UnauthorizedAccessException, $"Expected a sharing/access failure, got {error}");
        }
        Assert.Equal(ValidJson, File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(temp.Root, "*.tmp"));
    }

    [Fact]
    public void InvalidDataIsRejectedBeforeWriting()
    {
        using var temp = new TestDirectory();
        var path = temp.FilePath("settings.json");
        File.WriteAllText(path, ValidJson);
        Assert.Throws<InvalidDataException>(() => ConfigStore.Save(new AppSettings { Profiles = null! }, path));
        Assert.Equal(ValidJson, File.ReadAllText(path));
    }

    [Fact]
    public void ADeveloperSeedIsUsedOnlyWhenUserSettingsAreAbsent()
    {
        using var temp = new TestDirectory();
        var path = temp.FilePath("settings.json");
        var seed = temp.FilePath("seed.json");
        File.WriteAllText(seed, ValidJson);
        Assert.Single(ConfigStore.Load(path, seed, out var warning).Profiles);
        Assert.Null(warning);
        Assert.False(File.Exists(path));
        File.WriteAllText(path, "{broken");
        var fallback = ConfigStore.Load(path, seed, out warning);
        Assert.NotNull(warning);
        Assert.Equal(2, fallback.Profiles.Count);
        Assert.Equal("{broken", File.ReadAllText(path));
        Assert.Equal(ValidJson, File.ReadAllText(seed));
    }

    [Fact]
    public void ComputersWithDeletedProfilesRemainAvailableForRepair()
    {
        using var temp = new TestDirectory();
        var path = temp.FilePath("settings.json");
        var settings = ConfigStore.Load(path, null, out _);
        settings.Targets.Add(new() { Name = "Keep me", RustDeskId = "123456789", ProfileId = "deleted" });
        ConfigStore.Save(settings, path);
        Assert.Equal("deleted", Assert.Single(ConfigStore.Load(path, null, out var warning).Targets).ProfileId);
        Assert.Null(warning);
    }
}

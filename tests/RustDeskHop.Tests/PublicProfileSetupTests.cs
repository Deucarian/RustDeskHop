using Simultria.RustDeskCompanion;
using Xunit;

namespace RustDeskHop.Tests;

public sealed class PublicProfileSetupTests
{
    private static readonly string[] Options = ["custom-rendezvous-server", "relay-server", "api-server", "key"];
    private const string Original = """
        rendezvous_server = 'SCOPE.example:21116'
        theme = 'dark'
        [options]
        custom-rendezvous-server = 'SCOPE.example:21116'
        relay-server = 'SCOPE.example:21117'
        api-server = 'https://SCOPE.example'
        key = 'SCOPE-public-key'
        enable-file-transfer = 'Y'
        """;

    [Fact]
    public void SuccessfulSetupBacksUpBothScopesBeforeAnyCommand()
    {
        using var fixture = new Fixture();
        var commands = new List<(string Name, string Value)>();
        var result = RustDeskPublicProfileSetup.Prepare(fixture.User, fixture.Service, (name, value) =>
        {
            Assert.Equal(fixture.UserOriginal, File.ReadAllText(fixture.Backup(fixture.User)));
            Assert.Equal(fixture.ServiceOriginal, File.ReadAllText(fixture.Backup(fixture.Service)));
            commands.Add((name, value));
        });
        Assert.True(result.Success, result.Message);
        Assert.Equal(Options, commands.Select(c => c.Name));
        Assert.All(commands, c => Assert.Equal("", c.Value));
        foreach (var path in new[] { fixture.User, fixture.Service })
        {
            var updated = File.ReadAllText(path);
            Assert.DoesNotContain(".example", updated);
            Assert.Contains("theme = 'dark'", updated);
            Assert.Contains("enable-file-transfer = 'Y'", updated);
            Assert.False(File.Exists(path + ".rustdeskhop.tmp"));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LockedBackupIssuesNoConfigurationCommands(bool lockService)
    {
        using var fixture = new Fixture();
        var commands = new List<string>();
        using (var locked = new FileStream(lockService ? fixture.Service : fixture.User,
                   FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var result = RustDeskPublicProfileSetup.Prepare(fixture.User, fixture.Service, (name, _) => commands.Add(name));
            Assert.False(result.Success);
            Assert.Contains("No server settings were changed", result.Message);
            Assert.Empty(commands);
        }
        fixture.AssertOriginalFiles();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BackupDirectoryFailureIssuesNoConfigurationCommands(bool blockService)
    {
        using var fixture = new Fixture();
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(blockService ? fixture.Service : fixture.User)!, "RustDeskHop Backups"), "blocked");
        var calls = 0;
        var result = RustDeskPublicProfileSetup.Prepare(fixture.User, fixture.Service, (_, _) => calls++);
        Assert.False(result.Success);
        Assert.Equal(0, calls);
        fixture.AssertOriginalFiles();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void PartialCommandFailureRollsBackOnlyAttemptedOptionsAndRestoresDistinctFiles(int failingCommand)
    {
        using var fixture = new Fixture();
        var commands = new List<(string Name, string Value)>();
        var result = RustDeskPublicProfileSetup.Prepare(fixture.User, fixture.Service, (name, value) =>
        {
            commands.Add((name, value));
            // Model a CLI call that rewrites both files even when it subsequently fails.
            File.WriteAllText(fixture.User, "changed by management command");
            File.WriteAllText(fixture.Service, "changed by management command");
            if (commands.Count == failingCommand) throw new IOException("Injected command failure");
        });
        Assert.False(result.Success);
        Assert.Contains("was restored", result.Message);
        Assert.Equal(failingCommand * 2, commands.Count);
        Assert.Equal(Options.Take(failingCommand).Reverse(), commands.Skip(failingCommand).Select(c => c.Name));
        foreach (var command in commands.Skip(failingCommand))
            Assert.Equal(RustDeskTomlEditor.ReadSetting(fixture.ServiceOriginal, command.Name), command.Value);
        fixture.AssertOriginalFiles();
        Assert.Equal(fixture.UserOriginal, File.ReadAllText(fixture.Backup(fixture.User)));
        Assert.Equal(fixture.ServiceOriginal, File.ReadAllText(fixture.Backup(fixture.Service)));
    }

    [Fact]
    public void RollbackFailureStillRestoresFilesAndReportsActualBackupLocations()
    {
        using var fixture = new Fixture();
        var calls = 0;
        var result = RustDeskPublicProfileSetup.Prepare(fixture.User, fixture.Service, (_, _) =>
        {
            calls++;
            File.WriteAllText(fixture.User, "partial change");
            if (calls >= 2) throw new IOException("Injected failure during apply and rollback");
        });
        Assert.False(result.Success);
        Assert.Contains("restore was incomplete", result.Message);
        Assert.Contains(Path.GetDirectoryName(fixture.Backup(fixture.User))!, result.Message);
        Assert.Contains(Path.GetDirectoryName(fixture.Backup(fixture.Service))!, result.Message);
        fixture.AssertOriginalFiles();
    }

    [Fact]
    public void AFileLostDuringSetupIsRestoredFromItsSnapshot()
    {
        using var fixture = new Fixture();
        var calls = 0;
        var result = RustDeskPublicProfileSetup.Prepare(fixture.User, fixture.Service, (_, _) =>
        {
            if (++calls == 4) File.Delete(fixture.Service);
        });
        Assert.False(result.Success);
        Assert.Contains("was restored", result.Message);
        Assert.Equal(8, calls);
        fixture.AssertOriginalFiles();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARecordedSingleConfigurationScopeCanBePrepared(bool serviceOnly)
    {
        using var fixture = new Fixture();
        File.Delete(serviceOnly ? fixture.User : fixture.Service);
        var calls = 0;
        var result = RustDeskPublicProfileSetup.Prepare(fixture.User, fixture.Service, (_, _) => calls++);
        Assert.True(result.Success, result.Message);
        Assert.Equal(4, calls);
        Assert.False(File.Exists(serviceOnly ? fixture.User : fixture.Service));
    }

    [Fact]
    public void NoCapturedConfigurationMeansNoChanges()
    {
        using var temp = new TestDirectory();
        var calls = 0;
        var result = RustDeskPublicProfileSetup.Prepare(temp.FilePath("user.toml"), temp.FilePath("service.toml"), (_, _) => calls++);
        Assert.False(result.Success);
        Assert.Contains("No server settings were changed", result.Message);
        Assert.Equal(0, calls);
        Assert.Empty(Directory.GetFileSystemEntries(temp.Root));
    }

    [Fact]
    public void AbsentServiceOptionsAreNotRestoredFromUnrelatedUserValues()
    {
        using var fixture = new Fixture();
        File.WriteAllText(fixture.Service, "theme = 'light'");
        var commands = new List<(string Name, string Value)>();
        var result = RustDeskPublicProfileSetup.Prepare(fixture.User, fixture.Service, (name, value) =>
        {
            commands.Add((name, value));
            if (commands.Count == 4) throw new IOException("Fail after capturing service defaults");
        });
        Assert.False(result.Success);
        Assert.All(commands.Skip(4), c => Assert.Equal("", c.Value));
        Assert.Equal(fixture.UserOriginal, File.ReadAllText(fixture.User));
        Assert.Equal("theme = 'light'", File.ReadAllText(fixture.Service));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TestDirectory temp = new();
        internal string User { get; }
        internal string Service { get; }
        internal string UserOriginal => Original.Replace("SCOPE", "user");
        internal string ServiceOriginal => Original.Replace("SCOPE", "service");

        internal Fixture()
        {
            User = temp.FilePath("user/RustDesk2.toml");
            Service = temp.FilePath("service/RustDesk2.toml");
            Directory.CreateDirectory(Path.GetDirectoryName(User)!);
            Directory.CreateDirectory(Path.GetDirectoryName(Service)!);
            File.WriteAllText(User, UserOriginal);
            File.WriteAllText(Service, ServiceOriginal);
        }

        internal string Backup(string path) => Assert.Single(Directory.GetFiles(Path.Combine(Path.GetDirectoryName(path)!, "RustDeskHop Backups")));

        internal void AssertOriginalFiles()
        {
            Assert.Equal(UserOriginal, File.ReadAllText(User));
            Assert.Equal(ServiceOriginal, File.ReadAllText(Service));
        }

        public void Dispose() => temp.Dispose();
    }
}

using System.Net;
using System.Net.Sockets;
using Simultria.RustDeskCompanion;
using Xunit;

namespace RustDeskHop.Tests;

public sealed class RustDeskIntegrationTests
{
    [Fact]
    public void ServerProfileDisplaysItsFriendlyName()
    {
        var profile = new ServerProfile { Name = "Office private network" };

        Assert.Equal("Office private network", profile.ToString());
    }

    [Fact]
    public void BuildsPublicConnectionTarget()
    {
        var target = new TargetDefinition { RustDeskId = "123456789" };
        var profile = new ServerProfile { ServerAddress = "public" };

        Assert.Equal("123456789", ConnectionTargetBuilder.Build(target, profile, RustDeskDefaultRoute.Public));
    }

    [Theory]
    [InlineData((int)RustDeskDefaultRoute.Private)]
    [InlineData((int)RustDeskDefaultRoute.Unknown)]
    public void PublicConnectionRequiresAConfirmedPublicDefault(int route)
    {
        var target = new TargetDefinition { RustDeskId = "123456789" };
        var profile = new ServerProfile { ServerAddress = "public" };
        Assert.Throws<InvalidOperationException>(() => ConnectionTargetBuilder.Build(target, profile, (RustDeskDefaultRoute)route));
    }

    [Theory]
    [InlineData((int)RustDeskDefaultRoute.Public)]
    [InlineData((int)RustDeskDefaultRoute.Private)]
    [InlineData((int)RustDeskDefaultRoute.Unknown)]
    public void BuildsPrivateConnectionTargetWithKey(int route)
    {
        var target = new TargetDefinition { RustDeskId = "123456789" };
        var profile = new ServerProfile
        {
            ServerAddress = "rustdesk.example:21116",
            PublicKey = "abc+/=",
        };

        Assert.Equal(
            "123456789@rustdesk.example:21116?key=abc+/=",
            ConnectionTargetBuilder.Build(target, profile, (RustDeskDefaultRoute)route));
    }

    [Theory]
    [InlineData("123@public")]
    [InlineData("123?key=bad")]
    [InlineData("123&bad")]
    public void RejectsUnsafeRustDeskIds(string id)
    {
        var target = new TargetDefinition { RustDeskId = id };
        var profile = new ServerProfile { ServerAddress = "public" };

        Assert.Throws<InvalidOperationException>(() => ConnectionTargetBuilder.Build(target, profile, RustDeskDefaultRoute.Public));
    }

    [Theory]
    [InlineData("rs-ny.rustdesk.com:21116", (int)RustDeskDefaultRoute.Public)]
    [InlineData("RS-SG.RUSTDESK.COM:21116", (int)RustDeskDefaultRoute.Public)]
    [InlineData("public", (int)RustDeskDefaultRoute.Public)]
    [InlineData("", (int)RustDeskDefaultRoute.Public)]
    [InlineData("rs-private.example:21116", (int)RustDeskDefaultRoute.Private)]
    [InlineData("rs-ny.rustdesk.com.example:21116", (int)RustDeskDefaultRoute.Private)]
    [InlineData("private.example:21116", (int)RustDeskDefaultRoute.Private)]
    public void DefaultRouteUsesPublicHostsNotAPrefix(string server, int expected)
    {
        var path = WriteTemporaryFile($"rendezvous_server = '{server}'");
        try { Assert.Equal((RustDeskDefaultRoute)expected, RustDeskConfigReader.ReadDefaultRoute(path)); }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("rendezvous_server = 'unterminated")]
    [InlineData("rendezvous_server = 123")]
    [InlineData("rendezvous_server = 'public'\nrendezvous_server = 'private.example'")]
    public void MalformedServerConfigurationDoesNotEnablePublicRouting(string contents)
    {
        var path = WriteTemporaryFile(contents);
        try { Assert.Equal(RustDeskDefaultRoute.Unknown, RustDeskConfigReader.ReadDefaultRoute(path)); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MissingOrLockedConfigIsUnknownEvenWithAPublicSavedProfile()
    {
        var path = WriteTemporaryFile("rendezvous_server = 'public'");
        var settings = new AppSettings { Profiles = [new ServerProfile { ServerAddress = "public" }] };
        try
        {
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.Equal(RustDeskDefaultRoute.Unknown, RustDeskConfigReader.ReadDefaultRoute(path));
                Assert.Null(RustDeskConfigReader.DetectDefaultProfile(settings, path));
            }
            File.Delete(path);
            Assert.Equal(RustDeskDefaultRoute.Unknown, RustDeskConfigReader.ReadDefaultRoute(path));
            Assert.Null(RustDeskConfigReader.DetectDefaultProfile(settings, path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReadableBuiltInDefaultIsPublicWithoutFallingBackToAPrivateSavedProfile()
    {
        var path = WriteTemporaryFile("[options]\ntheme = 'dark'");
        try
        {
            Assert.Equal(RustDeskDefaultRoute.Public, RustDeskConfigReader.ReadDefaultRoute(path));
            Assert.Null(RustDeskConfigReader.DetectDefaultProfile(new AppSettings
            {
                Profiles = [new ServerProfile { ServerAddress = "private.example" }],
            }, path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void EffectiveRendezvousServerWinsOverStaleCustomOption()
    {
        var path = WriteTemporaryFile("""
            rendezvous_server = 'rs-ny.rustdesk.com:21116'

            [options]
            custom-rendezvous-server = 'private.example:21116'
            """);

        try
        {
            Assert.Equal("rs-ny.rustdesk.com:21116", RustDeskConfigReader.ReadConfiguredServer(path));
            Assert.Equal(RustDeskDefaultRoute.Public, RustDeskConfigReader.ReadDefaultRoute(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CustomServerIsUsedWhenEffectiveServerIsMissing()
    {
        var path = WriteTemporaryFile("""
            [options]
            custom-rendezvous-server = 'private.example:21116'
            """);

        try
        {
            Assert.Equal("private.example:21116", RustDeskConfigReader.ReadConfiguredServer(path));
            Assert.Equal(RustDeskDefaultRoute.Private, RustDeskConfigReader.ReadDefaultRoute(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DetectsAStoredRustDeskLoginWithoutReadingItOutsideTheProcess()
    {
        var path = WriteTemporaryFile("access_token = 'encrypted-token-value'");

        try
        {
            Assert.True(RustDeskAccountState.HasLoginToken(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EmptyLoginTokenIsNotTreatedAsSignedIn()
    {
        var path = WriteTemporaryFile("access_token = ''");

        try
        {
            Assert.False(RustDeskAccountState.HasLoginToken(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FreshSignInDoesNotAutomaticallyAcceptAnUnchangedCachedLogin()
    {
        string? fingerprint = "old-fingerprint";
        var attempt = new RustDeskSignInAttempt(() => fingerprint);
        Assert.False(attempt.CanContinue());
        Assert.True(attempt.CanContinue(userRequestedRetry: true));
        fingerprint = null;
        Assert.False(attempt.CanContinue());
        Assert.False(attempt.CanContinue(userRequestedRetry: true));
        fingerprint = "new-fingerprint";
        Assert.True(attempt.CanContinue());
    }

    [Fact]
    public void FreshSignInWithoutACacheWaitsForASavedLogin()
    {
        string? fingerprint = null;
        var attempt = new RustDeskSignInAttempt(() => fingerprint);
        Assert.False(attempt.CanContinue());
        fingerprint = "new-fingerprint";
        Assert.True(attempt.CanContinue());
    }

    [Fact]
    public void LoginObservationUsesOnlyATokenFingerprintNotUnrelatedFileChanges()
    {
        var path = WriteTemporaryFile("access_token = 'test-only-token'\ntheme = 'dark'");
        try
        {
            var fingerprint = RustDeskAccountState.ReadLoginFingerprint(path);
            Assert.NotNull(fingerprint);
            Assert.DoesNotContain("test-only-token", fingerprint);
            var attempt = new RustDeskSignInAttempt(() => RustDeskAccountState.ReadLoginFingerprint(path));
            File.WriteAllText(path, "access_token = 'test-only-token'\ntheme = 'light'");
            Assert.False(attempt.CanContinue());
            File.WriteAllText(path, "access_token = 'new-test-only-token'");
            Assert.True(attempt.CanContinue());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void PublicPreparationRemovesOnlyServerSpecificTomlValues()
    {
        var original = """
            rendezvous_server = 'private.example:21116'
            theme = 'dark'

            [options]
            custom-rendezvous-server = 'private.example:21116'
            relay-server = 'private.example:21117'
            api-server = 'https://private.example'
            key = 'public-key'
            enable-file-transfer = 'Y'
            """;

        var updated = RustDeskTomlEditor.ClearCustomServer(original);

        Assert.DoesNotContain("private.example", updated);
        Assert.DoesNotContain("key =", updated);
        Assert.Contains("theme = 'dark'", updated);
        Assert.Contains("enable-file-transfer = 'Y'", updated);
    }

    [Fact]
    public void ReadsServerOptionForRollback()
    {
        const string contents = """
            [options]
            custom-rendezvous-server = 'private.example:21116'
            key = "abc+/="
            """;

        Assert.Equal("private.example:21116", RustDeskTomlEditor.ReadSetting(contents, "custom-rendezvous-server"));
        Assert.Equal("abc+/=", RustDeskTomlEditor.ReadSetting(contents, "key"));
        Assert.Null(RustDeskTomlEditor.ReadSetting(contents, "api-server"));
    }

    [Fact]
    public async Task PrivateNetworkProbeConnectsToReachableServer()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var acceptTask = listener.AcceptTcpClientAsync();

        try
        {
            var profile = new ServerProfile
            {
                RequiresPrivateNetwork = true,
                ProbeHost = IPAddress.Loopback.ToString(),
                ProbePort = port,
            };

            Assert.True(await NetworkProbe.CanReachAsync(profile));
            using var accepted = await acceptTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            listener.Stop();
        }
    }

    private static string WriteTemporaryFile(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"RustDeskHop-{Guid.NewGuid():N}.toml");
        File.WriteAllText(path, contents);
        return path;
    }
}

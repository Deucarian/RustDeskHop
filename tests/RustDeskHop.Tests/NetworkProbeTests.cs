using System.Net;
using System.Net.Sockets;
using Simultria.RustDeskCompanion;
using Xunit;

namespace RustDeskHop.Tests;

public sealed class NetworkProbeTests
{
    [Theory]
    [InlineData("rd.example", "rd.example")]
    [InlineData(" rd.example:21116 ", "rd.example")]
    [InlineData("192.0.2.1:21116", "192.0.2.1")]
    [InlineData("192.0.2.1", "192.0.2.1")]
    [InlineData("[::1]:21116", "::1")]
    [InlineData("[::1]", "::1")]
    [InlineData("::1", "::1")]
    [InlineData("2001:db8::2116", "2001:db8::2116")]
    [InlineData("[fe80::1%12]:21116", "fe80::1%12")]
    public void ExtractsDnsIpv4AndIpv6Hosts(string address, string expected)
    {
        Assert.Equal(expected, NetworkProbe.GetProbeHost(new() { ServerAddress = address }));
    }

    [Theory]
    [InlineData("[::1")]
    [InlineData("[not-an-ip]:21116")]
    [InlineData("[::1]junk")]
    [InlineData("[::1]:65536")]
    [InlineData("host:abc")]
    [InlineData("host:0")]
    [InlineData("https://rd.example")]
    [InlineData("")]
    public async Task MalformedAddressesFailWithoutThrowing(string address)
    {
        Assert.False(await NetworkProbe.CanReachAsync(new() { RequiresPrivateNetwork = true, ServerAddress = address }));
    }

    [Fact]
    public void ExplicitProbeHostWinsOverTheServerAddress()
    {
        Assert.Equal("::1", NetworkProbe.GetProbeHost(new() { ServerAddress = "wrong.invalid:21116", ProbeHost = " [::1] " }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReachableIpv6ServerWorksWithAndWithoutProbeOverride(bool overrideHost)
    {
        var listener = new TcpListener(IPAddress.IPv6Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var accept = listener.AcceptTcpClientAsync();
            var profile = new ServerProfile
            {
                RequiresPrivateNetwork = true,
                ServerAddress = overrideHost ? "not-used.invalid:21116" : $"[::1]:{port}",
                ProbeHost = overrideHost ? "::1" : "",
                ProbePort = port,
            };
            Assert.True(await NetworkProbe.CanReachAsync(profile));
            using var peer = await accept.WaitAsync(TimeSpan.FromSeconds(3));
        }
        finally { listener.Stop(); }
    }

    [Fact]
    public async Task ClosedPortAndCancellationReturnFalse()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var profile = new ServerProfile { RequiresPrivateNetwork = true, ServerAddress = "127.0.0.1", ProbePort = port };
        Assert.False(await NetworkProbe.CanReachAsync(profile));
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        Assert.False(await NetworkProbe.CanReachAsync(profile, cancel.Token));
    }

    [Fact]
    public async Task PublicProfilesDoNotRequireANetworkProbe()
    {
        Assert.True(await NetworkProbe.CanReachAsync(new() { ServerAddress = "public" }));
    }
}

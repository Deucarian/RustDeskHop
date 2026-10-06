using System.Net;
using System.Net.Sockets;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class NetworkProbeTests
    {
        #region Test Methods
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
            Assert.Equal(expected, NetworkProbe.GetProbeHost(new ServerProfile() { ServerAddress = address }));
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
            Assert.False(await NetworkProbe.CanReachAsync(new ServerProfile()
                                                              { RequiresPrivateNetwork = true, ServerAddress = address }
                                                         )
                        );
        }

        [Fact]
        public void ExplicitProbeHostWinsOverTheServerAddress()
        {
            Assert.Equal("::1",
                         NetworkProbe.GetProbeHost(new ServerProfile()
                                                       { ServerAddress = "wrong.invalid:21116", ProbeHost = " [::1] " }
                                                  )
                        );
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReachableIpv6ServerWorksWithAndWithoutProbeOverride(bool overrideHost)
        {
            TcpListener listener = new TcpListener(IPAddress.IPv6Loopback, 0);
            listener.Start();
            try
            {
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Task<TcpClient> accept = listener.AcceptTcpClientAsync();
                ServerProfile profile = new ServerProfile
                {
                    RequiresPrivateNetwork = true,
                    ServerAddress = overrideHost ? "not-used.invalid:21116" : $"[::1]:{port}",
                    ProbeHost = overrideHost ? "::1" : "",
                    ProbePort = port,
                };
                Assert.True(await NetworkProbe.CanReachAsync(profile));
                using TcpClient peer = await accept.WaitAsync(TimeSpan.FromSeconds(3));
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ClosedPortAndCancellationReturnFalse()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            ServerProfile profile = new ServerProfile
            {
                RequiresPrivateNetwork = true,
                ServerAddress = "127.0.0.1",
                ProbePort = port
            };
            Assert.False(await NetworkProbe.CanReachAsync(profile));
            using CancellationTokenSource cancel = new CancellationTokenSource();
            cancel.Cancel();
            Assert.False(await NetworkProbe.CanReachAsync(profile, cancel.Token));
        }

        [Fact]
        public async Task PublicProfilesDoNotRequireANetworkProbe()
        {
            Assert.True(await NetworkProbe.CanReachAsync(new ServerProfile() { ServerAddress = "public" }));
        }
        #endregion
    }
}
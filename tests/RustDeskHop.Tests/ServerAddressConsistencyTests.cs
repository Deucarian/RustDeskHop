using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class ServerAddressConsistencyTests
    {
        #region Test Methods
        [Theory]
        [InlineData("[::1]:21116", "::1")]
        [InlineData("::1", "[::1]:21116")]
        [InlineData("[2001:db8::1]:21116", "2001:db8::1")]
        [InlineData("private.example:21116", "PRIVATE.EXAMPLE")]
        [InlineData("192.0.2.1:21116", "192.0.2.1")]
        public void RouteDetectionAndNetworkProbeUseTheSameHost(string configured, string saved)
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("RustDesk2.toml");
            File.WriteAllText(path, $"rendezvous_server = '{configured}'");
            ServerProfile profile = new ServerProfile
            {
                Id = "private",
                ServerAddress = saved
            };
            AppSettings settings = new AppSettings
            {
                Profiles =
                [
                    new ServerProfile()
                    {
                        ServerAddress = "public"
                    },
                    profile
                ]
            };
            Assert.Equal(RustDeskDefaultRoute.PRIVATE, RustDeskConfigReader.ReadDefaultRoute(path));
            Assert.Same(profile, RustDeskConfigReader.DetectDefaultProfile(settings, path));
            Assert.Equal(ServerAddressParser.GetHost(configured), NetworkProbe.GetProbeHost(profile), ignoreCase: true);
        }

        [Theory]
        [InlineData("rs-ny.rustdesk.com:bad")]
        [InlineData("rs-ny.rustdesk.com:65536")]
        [InlineData("[not-an-ip]:21116")]
        [InlineData("https://private.example")]
        public void InvalidEndpointsCannotBeClassifiedAsConfirmedPublicOrPrivate(string configured)
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("RustDesk2.toml");
            File.WriteAllText(path, $"rendezvous_server = '{configured}'");
            Assert.Equal(RustDeskDefaultRoute.UNKNOWN, RustDeskConfigReader.ReadDefaultRoute(path));
            Assert.Null(RustDeskConfigReader.DetectDefaultProfile(new AppSettings()
                                                                  {
                                                                      Profiles =
                                                                      [
                                                                          new ServerProfile()
                                                                              { ServerAddress = "public" },
                                                                          new ServerProfile()
                                                                              { ServerAddress = configured }
                                                                      ],
                                                                  },
                                                                  path
                                                                 )
                       );
        }

        [Fact]
        public void InvalidSavedExampleDoesNotPreventMatchingAValidPrivateProfile()
        {
            using TestDirectory temp = new TestDirectory();
            string path = temp.FilePath("RustDesk2.toml");
            File.WriteAllText(path, "rendezvous_server = '[::1]:21116'");
            ServerProfile profile = new ServerProfile
            {
                ServerAddress = "::1"
            };
            Assert.Same(profile,
                        RustDeskConfigReader.DetectDefaultProfile(new AppSettings()
                                                                  {
                                                                      Profiles =
                                                                      [
                                                                          new ServerProfile()
                                                                              { ServerAddress = "[unfinished" },
                                                                          profile
                                                                      ],
                                                                  },
                                                                  path
                                                                 )
                       );
        }
        #endregion
    }
}
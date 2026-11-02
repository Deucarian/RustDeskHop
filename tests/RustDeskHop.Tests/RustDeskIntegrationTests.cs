using System.Net;
using System.Net.Sockets;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class RustDeskIntegrationTests
    {
        #region Test Methods
        [Fact]
        public void ServerProfileDisplaysItsFriendlyName()
        {
            ServerProfile profile = new ServerProfile
            {
                Name = "Office private network"
            };
            Assert.Equal("Office private network", profile.ToString());
        }

        [Fact]
        public void BuildsPublicConnectionTarget()
        {
            TargetDefinition target = new TargetDefinition
            {
                RustDeskId = "123456789"
            };
            ServerProfile profile = new ServerProfile
            {
                ServerAddress = "public"
            };
            Assert.Equal("123456789", ConnectionTargetBuilder.Build(target, profile, RustDeskDefaultRoute.PUBLIC));
        }

        [Theory]
        [InlineData((int)RustDeskDefaultRoute.PRIVATE)]
        [InlineData((int)RustDeskDefaultRoute.UNKNOWN)]
        public void PublicConnectionRequiresAConfirmedPublicDefault(int route)
        {
            TargetDefinition target = new TargetDefinition
            {
                RustDeskId = "123456789"
            };
            ServerProfile profile = new ServerProfile
            {
                ServerAddress = "public"
            };
            Assert.Throws<InvalidOperationException>(() => ConnectionTargetBuilder.Build(target,
                                                              profile,
                                                              (RustDeskDefaultRoute)route
                                                         )
                                                    );
        }

        [Theory]
        [InlineData((int)RustDeskDefaultRoute.PUBLIC)]
        [InlineData((int)RustDeskDefaultRoute.PRIVATE)]
        [InlineData((int)RustDeskDefaultRoute.UNKNOWN)]
        public void BuildsPrivateConnectionTargetWithKey(int route)
        {
            TargetDefinition target = new TargetDefinition
            {
                RustDeskId = "123456789"
            };
            ServerProfile profile = new ServerProfile
            {
                ServerAddress = "rustdesk.example:21116",
                PublicKey = "abc+/=",
            };
            Assert.Equal("123456789@rustdesk.example:21116?key=abc+/=",
                         ConnectionTargetBuilder.Build(target, profile, (RustDeskDefaultRoute)route)
                        );
        }

        [Theory]
        [InlineData("123@public")]
        [InlineData("123?key=bad")]
        [InlineData("123&bad")]
        public void RejectsUnsafeRustDeskIds(string id)
        {
            TargetDefinition target = new TargetDefinition
            {
                RustDeskId = id
            };
            ServerProfile profile = new ServerProfile
            {
                ServerAddress = "public"
            };
            Assert.Throws<InvalidOperationException>(() => ConnectionTargetBuilder.Build(target,
                                                              profile,
                                                              RustDeskDefaultRoute.PUBLIC
                                                         )
                                                    );
        }

        [Theory]
        [InlineData("rs-ny.rustdesk.com:21116", (int)RustDeskDefaultRoute.PUBLIC)]
        [InlineData("RS-SG.RUSTDESK.COM:21116", (int)RustDeskDefaultRoute.PUBLIC)]
        [InlineData("public", (int)RustDeskDefaultRoute.PUBLIC)]
        [InlineData("", (int)RustDeskDefaultRoute.PUBLIC)]
        [InlineData("rs-private.example:21116", (int)RustDeskDefaultRoute.PRIVATE)]
        [InlineData("rs-ny.rustdesk.com.example:21116", (int)RustDeskDefaultRoute.PRIVATE)]
        [InlineData("private.example:21116", (int)RustDeskDefaultRoute.PRIVATE)]
        public void DefaultRouteUsesPublicHostsNotAPrefix(string server, int expected)
        {
            string path = WriteTemporaryFile($"rendezvous_server = '{server}'");
            try
            {
                Assert.Equal((RustDeskDefaultRoute)expected, RustDeskConfigReader.ReadDefaultRoute(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Theory]
        [InlineData("rendezvous_server = 'unterminated")]
        [InlineData("rendezvous_server = 123")]
        [InlineData("rendezvous_server = 'public'\nrendezvous_server = 'private.example'")]
        public void MalformedServerConfigurationDoesNotEnablePublicRouting(string contents)
        {
            string path = WriteTemporaryFile(contents);
            try
            {
                Assert.Equal(RustDeskDefaultRoute.UNKNOWN, RustDeskConfigReader.ReadDefaultRoute(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void MissingOrLockedConfigIsUnknownEvenWithAPublicSavedProfile()
        {
            string path = WriteTemporaryFile("rendezvous_server = 'public'");
            AppSettings settings = new AppSettings
            {
                Profiles =
                [
                    new ServerProfile
                    {
                        ServerAddress = "public"
                    }
                ]
            };
            try
            {
                using (FileStream locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.Equal(RustDeskDefaultRoute.UNKNOWN, RustDeskConfigReader.ReadDefaultRoute(path));
                    Assert.Null(RustDeskConfigReader.DetectDefaultProfile(settings, path));
                }

                File.Delete(path);
                Assert.Equal(RustDeskDefaultRoute.UNKNOWN, RustDeskConfigReader.ReadDefaultRoute(path));
                Assert.Null(RustDeskConfigReader.DetectDefaultProfile(settings, path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ReadableBuiltInDefaultIsPublicWithoutFallingBackToAPrivateSavedProfile()
        {
            string path = WriteTemporaryFile("[options]\ntheme = 'dark'");
            try
            {
                Assert.Equal(RustDeskDefaultRoute.PUBLIC, RustDeskConfigReader.ReadDefaultRoute(path));
                Assert.Null(RustDeskConfigReader.DetectDefaultProfile(new AppSettings
                                                                      {
                                                                          Profiles =
                                                                          [
                                                                              new ServerProfile
                                                                                  { ServerAddress = "private.example" }
                                                                          ],
                                                                      },
                                                                      path
                                                                     )
                           );
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void EffectiveRendezvousServerWinsOverStaleCustomOption()
        {
            string path = WriteTemporaryFile("""
                                             rendezvous_server = 'rs-ny.rustdesk.com:21116'

                                             [options]
                                             custom-rendezvous-server = 'private.example:21116'
                                             """
                                            );
            try
            {
                Assert.Equal("rs-ny.rustdesk.com:21116", RustDeskConfigReader.ReadConfiguredServer(path));
                Assert.Equal(RustDeskDefaultRoute.PUBLIC, RustDeskConfigReader.ReadDefaultRoute(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void CustomServerIsUsedWhenEffectiveServerIsMissing()
        {
            string path = WriteTemporaryFile("""
                                             [options]
                                             custom-rendezvous-server = 'private.example:21116'
                                             """
                                            );
            try
            {
                Assert.Equal("private.example:21116", RustDeskConfigReader.ReadConfiguredServer(path));
                Assert.Equal(RustDeskDefaultRoute.PRIVATE, RustDeskConfigReader.ReadDefaultRoute(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void DetectsAStoredRustDeskLoginWithoutReadingItOutsideTheProcess()
        {
            string path = WriteTemporaryFile("access_token = 'encrypted-token-value'");
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
            string path = WriteTemporaryFile("access_token = ''");
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
            RustDeskSignInAttempt attempt = new RustDeskSignInAttempt(() => fingerprint);
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
            RustDeskSignInAttempt attempt = new RustDeskSignInAttempt(() => fingerprint);
            Assert.False(attempt.CanContinue());
            fingerprint = "new-fingerprint";
            Assert.True(attempt.CanContinue());
        }

        [Fact]
        public void LoginObservationUsesOnlyATokenFingerprintNotUnrelatedFileChanges()
        {
            string path = WriteTemporaryFile("access_token = 'test-only-token'\ntheme = 'dark'");
            try
            {
                string? fingerprint = RustDeskAccountState.ReadLoginFingerprint(path);
                Assert.NotNull(fingerprint);
                Assert.DoesNotContain("test-only-token", fingerprint);
                RustDeskSignInAttempt attempt =
                    new RustDeskSignInAttempt(() => RustDeskAccountState.ReadLoginFingerprint(path));
                File.WriteAllText(path, "access_token = 'test-only-token'\ntheme = 'light'");
                Assert.False(attempt.CanContinue());
                File.WriteAllText(path, "access_token = 'new-test-only-token'");
                Assert.True(attempt.CanContinue());
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void PublicPreparationRemovesOnlyServerSpecificTomlValues()
        {
            string original = """
                              rendezvous_server = 'private.example:21116'
                              theme = 'dark'

                              [options]
                              custom-rendezvous-server = 'private.example:21116'
                              relay-server = 'private.example:21117'
                              api-server = 'https://private.example'
                              key = 'public-key'
                              enable-file-transfer = 'Y'
                              """;
            string updated = RustDeskTomlEditor.ClearCustomServer(original);
            Assert.DoesNotContain("private.example", updated);
            Assert.DoesNotContain("key =", updated);
            Assert.Contains("theme = 'dark'", updated);
            Assert.Contains("enable-file-transfer = 'Y'", updated);
        }

        [Fact]
        public void ReadsServerOptionForRollback()
        {
            const string CONTENTS = """
                                    [options]
                                    custom-rendezvous-server = 'private.example:21116'
                                    key = "abc+/="
                                    """;
            Assert.Equal("private.example:21116", RustDeskTomlEditor.ReadSetting(CONTENTS, "custom-rendezvous-server"));
            Assert.Equal("abc+/=", RustDeskTomlEditor.ReadSetting(CONTENTS, "key"));
            Assert.Null(RustDeskTomlEditor.ReadSetting(CONTENTS, "api-server"));
        }

        [Fact]
        public async Task PrivateNetworkProbeConnectsToReachableServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
            try
            {
                ServerProfile profile = new ServerProfile
                {
                    RequiresPrivateNetwork = true,
                    ProbeHost = IPAddress.Loopback.ToString(),
                    ProbePort = port,
                };
                Assert.True(await NetworkProbe.CanReachAsync(profile));
                using TcpClient accepted = await acceptTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
            finally
            {
                listener.Stop();
            }
        }
        #endregion

        #region Methods
        private static string WriteTemporaryFile(string contents)
        {
            string path = Path.Combine(Path.GetTempPath(), $"RustDeskHop-{Guid.NewGuid():N}.toml");
            File.WriteAllText(path, contents);
            return path;
        }
        #endregion
    }
}
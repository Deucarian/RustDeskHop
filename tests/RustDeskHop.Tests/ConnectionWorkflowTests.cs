using System.Text.Json;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class ConnectionWorkflowTests
    {
        #region Test Methods
        [Fact]
        public async Task PublicUsesBareIdWithoutPreparationVpnOrFreshSignInWhenLoginIsCached()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Public));
            Assert.Equal(["123456789"], rig.RustDesk.LaunchedTargets);
            Assert.Equal(["find", "route", "route", "connect"], rig.Events);
        }

        [Theory]
        [InlineData((int)RustDeskDefaultRoute.PUBLIC)]
        [InlineData((int)RustDeskDefaultRoute.PRIVATE)]
        [InlineData((int)RustDeskDefaultRoute.UNKNOWN)]
        public async Task PrivateUsesExplicitRouteWithoutPreparingPublicOrClosingSessions(int route)
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.Route = (RustDeskDefaultRoute)route;
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Private));
            Assert.Equal(["123456789@private.example:21116?key=abc+/="], rig.RustDesk.LaunchedTargets);
            Assert.DoesNotContain("prepare", rig.Events);
            Assert.DoesNotContain("sign-in", rig.Events);
            Assert.DoesNotContain("start-vpn", rig.Events);
        }

        [Fact]
        public async Task DecliningRouteDoesNotProbeLaunchOrChangeSettings()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            string before = JsonSerializer.Serialize(rig.Settings);
            rig.Interaction.RouteConsent = false;
            Assert.Equal(ConnectionOutcome.CANCELLED, await rig.Connect(rig.Private));
            Assert.Equal(["confirm-route"], rig.Events);
            Assert.Equal(before, JsonSerializer.Serialize(rig.Settings));
        }

        [Fact]
        public async Task MissingAssignmentBlocksBeforeAnyExternalWork()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            Assert.Equal(ConnectionOutcome.BLOCKED, await rig.Connect(new ServerProfile() { Id = "deleted" }));
            Assert.Empty(rig.Events);
            Assert.Equal("Configuration needed", Assert.Single(rig.Interaction.Messages).Title);
        }

        [Fact]
        public async Task MatchingPrivateDefaultDoesNotAskForAnotherRoute()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.CurrentProfile = rig.Private;
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Private));
            Assert.DoesNotContain("confirm-route", rig.Events);
        }

        [Fact]
        public async Task UnknownDefaultStillAllowsExplicitPrivateRoute()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.Route = RustDeskDefaultRoute.UNKNOWN;
            rig.RustDesk.CurrentProfile = null;
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Private));
            Assert.DoesNotContain("confirm-route", rig.Events);
        }

        [Fact]
        public async Task ReachablePrivateServerDoesNotNeedVpnStartup()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Private));
            Assert.Equal(1, rig.Events.Count(e => e == "probe"));
            Assert.DoesNotContain("start-vpn", rig.Events);
        }

        [Fact]
        public async Task UnreachableServerStartsInstalledVpnAndRetriesOnce()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.Probe.Responses.Enqueue(false);
            rig.Probe.Responses.Enqueue(true);
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Private));
            Assert.Equal(["confirm-route", "probe", "start-vpn", "delay:2000", "probe", "find", "route", "connect"],
                         rig.Events
                        );
        }

        [Fact]
        public async Task FailedRetryNeverLaunchesRustDesk()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.Probe.Responses.Enqueue(false);
            rig.Probe.Responses.Enqueue(false);
            Assert.Equal(ConnectionOutcome.BLOCKED, await rig.Connect(rig.Private));
            Assert.Equal(2, rig.Events.Count(e => e == "probe"));
            Assert.DoesNotContain("find", rig.Events);
            Assert.Empty(rig.RustDesk.LaunchedTargets);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RunningOrUnavailableVpnDoesNotCauseRepeatedRetries(bool running)
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.Vpn.Running = running;
            rig.Vpn.StartResult = false;
            rig.Probe.Responses.Enqueue(false);
            Assert.Equal(ConnectionOutcome.BLOCKED, await rig.Connect(rig.Private));
            Assert.Equal(1, rig.Events.Count(e => e == "probe"));
            Assert.DoesNotContain("delay:2000", rig.Events);
            Assert.Empty(rig.RustDesk.LaunchedTargets);
        }

        [Fact]
        public async Task PrivateRouteWithoutProbeRequirementSkipsVpnAndProbe()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.Private.RequiresPrivateNetwork = false;
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Private));
            Assert.DoesNotContain("probe", rig.Events);
            Assert.DoesNotContain("start-vpn", rig.Events);
        }

        [Fact]
        public async Task MissingRustDeskStopsBeforeSignInOrPreparation()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.Executable = null;
            rig.RustDesk.Route = RustDeskDefaultRoute.PRIVATE;
            Assert.Equal(ConnectionOutcome.BLOCKED, await rig.Connect(rig.Public));
            Assert.Equal(["find"], rig.Events);
            Assert.Equal("RustDesk not found", Assert.Single(rig.Interaction.Messages).Title);
        }

        [Fact]
        public async Task UnknownPublicDefaultFailsClosedEvenWithCachedLogin()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.Route = RustDeskDefaultRoute.UNKNOWN;
            Assert.Equal(ConnectionOutcome.BLOCKED, await rig.Connect(rig.Public));
            Assert.Empty(rig.RustDesk.LaunchedTargets);
            Assert.DoesNotContain("prepare", rig.Events);
            Assert.DoesNotContain("sign-in", rig.Events);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PublicWithoutCachedLoginUsesUserSignInWithoutPreparation(bool accepted)
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.HasToken = false;
            rig.Interaction.SignInResult = accepted;
            Assert.Equal(accepted ? ConnectionOutcome.STARTED : ConnectionOutcome.BLOCKED,
                         await rig.Connect(rig.Public)
                        );
            Assert.Contains("sign-in", rig.Events);
            Assert.DoesNotContain("prepare", rig.Events);
            Assert.Equal(accepted ? 1 : 0, rig.RustDesk.LaunchedTargets.Count);
        }

        [Fact]
        public async Task DecliningPublicPreparationDoesNotCloseSessionsOrMutateDefault()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.Route = RustDeskDefaultRoute.PRIVATE;
            rig.Interaction.PreparationConsent = false;
            Assert.Equal(ConnectionOutcome.BLOCKED, await rig.Connect(rig.Public));
            Assert.Equal(["find", "route", "confirm-prepare"], rig.Events);
            Assert.Equal(RustDeskDefaultRoute.PRIVATE, rig.RustDesk.Route);
        }

        [Fact]
        public async Task PreparationRunsOnlyAfterConsentThenSignInAndFinalRouteCheck()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.Route = RustDeskDefaultRoute.PRIVATE;
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Public));
            Assert.Equal(["find", "route", "confirm-prepare", "prepare", "sign-in", "route", "connect"], rig.Events);
        }

        [Fact]
        public async Task FailedPreparationStopsBeforeSignInAndReportsItsReason()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.Route = RustDeskDefaultRoute.PRIVATE;
            rig.Preparation.Result = new PublicSetupResult(false, "Backup failed; no settings changed.");
            rig.Preparation.OnPrepare = null;
            Assert.Equal(ConnectionOutcome.BLOCKED, await rig.Connect(rig.Public));
            Assert.DoesNotContain("sign-in", rig.Events);
            Assert.Empty(rig.RustDesk.LaunchedTargets);
            Assert.Contains("Backup failed", Assert.Single(rig.Interaction.Messages).Message);
        }

        [Theory]
        [InlineData((int)RustDeskDefaultRoute.PRIVATE)]
        [InlineData((int)RustDeskDefaultRoute.UNKNOWN)]
        public async Task DefaultChangingDuringSignInBlocksBareIdLaunch(int route)
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.HasToken = false;
            rig.Interaction.OnSignIn = () => rig.RustDesk.Route = (RustDeskDefaultRoute)route;
            Assert.Equal(ConnectionOutcome.FAILED, await rig.Connect(rig.Public));
            Assert.Empty(rig.RustDesk.LaunchedTargets);
            Assert.DoesNotContain("prepare", rig.Events);
        }

        [Fact]
        public async Task PreparationClaimingSuccessCannotBypassFinalRouteGuard()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.Route = RustDeskDefaultRoute.PRIVATE;
            rig.Preparation.OnPrepare = null;
            Assert.Equal(ConnectionOutcome.FAILED, await rig.Connect(rig.Public));
            Assert.Empty(rig.RustDesk.LaunchedTargets);
        }

        [Fact]
        public async Task LaunchFailureIsReportedWithoutChangingConfiguration()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            string before = JsonSerializer.Serialize(rig.Settings);
            rig.RustDesk.LaunchError = new IOException("Launch failed");
            Assert.Equal(ConnectionOutcome.FAILED, await rig.Connect(rig.Private));
            Assert.Contains("Launch failed", Assert.Single(rig.Interaction.Messages).Message);
            Assert.Equal(before, JsonSerializer.Serialize(rig.Settings));
        }

        [Fact]
        public async Task ProbeFailureIsReportedAndDoesNotLaunch()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.Probe.OnProbe = () => throw new IOException("Probe failed");
            Assert.Equal(ConnectionOutcome.FAILED, await rig.Connect(rig.Private));
            Assert.Empty(rig.RustDesk.LaunchedTargets);
        }

        [Fact]
        public async Task InvalidIdNeverReachesLauncher()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            rig.RustDesk.HasToken = false;
            rig.RustDesk.Route = RustDeskDefaultRoute.PRIVATE;
            Assert.Equal(ConnectionOutcome.BLOCKED, await rig.Connect(rig.Public, "123@private"));
            Assert.Empty(rig.RustDesk.LaunchedTargets);
            Assert.DoesNotContain("prepare", rig.Events);
            Assert.DoesNotContain("sign-in", rig.Events);
        }

        [Fact]
        public async Task PublicPrivatePublicRoundTripOnlyLaunchesAndPreservesSettings()
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            string before = JsonSerializer.Serialize(rig.Settings);
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Public, "111"));
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Private, "222"));
            Assert.Equal(ConnectionOutcome.STARTED, await rig.Connect(rig.Public, "333"));
            Assert.Equal(["111", "222@private.example:21116?key=abc+/=", "333"], rig.RustDesk.LaunchedTargets);
            Assert.DoesNotContain("prepare", rig.Events);
            Assert.Equal(before, JsonSerializer.Serialize(rig.Settings));
        }
        #endregion
    }
}
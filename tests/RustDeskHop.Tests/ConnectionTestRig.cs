namespace RustDeskHop.Tests
{
    internal sealed class ConnectionTestRig
    {
        #region Constructors and Destructors
        internal ConnectionTestRig()
        {
            Settings = new AppSettings()
            {
                Profiles = [Public, Private]
            };
            RustDesk = new FakeRustDesk(Events)
            {
                CurrentProfile = Public
            };
            Probe = new FakeNetworkProbe(Events);
            Vpn = new FakeVpn(Events);
            Preparation = new FakePreparation(Events)
            {
                OnPrepare = () => RustDesk.Route = RustDeskDefaultRoute.PUBLIC
            };
            Interaction = new FakeInteraction(Events);
            Workflow = new ConnectionCoordinator(RustDesk,
                                                 RustDesk,
                                                 new PrivateNetworkAccess(Probe,
                                                                          Vpn,
                                                                          duration =>
                                                                          {
                                                                              Events
                                                                                  .Add($"delay:{duration.TotalMilliseconds}"
                                                                                      );
                                                                              return Task.CompletedTask;
                                                                          }
                                                                         ),
                                                 new PublicSignInService(RustDesk, Preparation)
                                                );
        }
        #endregion

        #region Properties and Indexers
        internal List<string> Events { get; } = [];

        internal ServerProfile Public { get; } = new ServerProfile()
        {
            Id = "public",
            Name = "Public",
            ServerAddress = "public"
        };

        internal ServerProfile Private { get; } = new ServerProfile()
        {
            Id = "private",
            Name = "Private",
            ServerAddress = "private.example:21116",
            PublicKey = "abc+/=",
            RequiresPrivateNetwork = true,
        };

        internal AppSettings Settings { get; }
        internal FakeRustDesk RustDesk { get; }
        internal FakeNetworkProbe Probe { get; }
        internal FakeVpn Vpn { get; }
        internal FakePreparation Preparation { get; }
        internal FakeInteraction Interaction { get; }
        internal ConnectionCoordinator Workflow { get; }
        #endregion

        #region Methods
        internal Task<ConnectionOutcome> Connect(ServerProfile profile, string id = "123456789") =>
            Workflow.ConnectAsync(new TargetDefinition()
                                      { Name = "Test computer", RustDeskId = id, ProfileId = profile.Id },
                                  Settings,
                                  Interaction
                                 );
        #endregion
    }

    internal sealed class FakeRustDesk(List<string> events) : IRustDeskClient, IRustDeskState
    {
        #region Properties and Indexers
        internal string? Executable { get; set; } = "test-only-rustdesk.exe";
        internal RustDeskDefaultRoute Route { get; set; } = RustDeskDefaultRoute.PUBLIC;
        internal ServerProfile? CurrentProfile { get; set; }
        internal bool HasToken { get; set; } = true;
        internal string? Fingerprint { get; set; }
        internal Exception? LaunchError { get; set; }
        internal List<string> LaunchedTargets { get; } = [];
        #endregion

        #region Methods
        public string? FindExecutable()
        {
            events.Add("find");
            return Executable;
        }

        public RustDeskDefaultRoute ReadDefaultRoute()
        {
            events.Add("route");
            return Route;
        }

        public ServerProfile? DetectDefaultProfile(AppSettings settings) => CurrentProfile;
        public bool HasLoginToken() => HasToken;
        public string? ReadLoginFingerprint() => Fingerprint;
        public void Open(string executablePath) => events.Add("open");

        public void Connect(string executablePath, string target)
        {
            events.Add("connect");
            if (LaunchError is not null)
                throw LaunchError;

            LaunchedTargets.Add(target);
        }
        #endregion
    }

    internal sealed class FakeNetworkProbe(List<string> events) : INetworkProbe
    {
        #region Properties and Indexers
        internal Queue<bool> Responses { get; } = [];
        internal Action? OnProbe { get; set; }
        #endregion

        #region Methods
        public Task<bool> CanReachAsync(ServerProfile profile)
        {
            events.Add("probe");
            OnProbe?.Invoke();
            return Task.FromResult(Responses.Count == 0 || Responses.Dequeue());
        }
        #endregion
    }

    internal sealed class FakeVpn(List<string> events) : IVpnClient
    {
        #region Properties and Indexers
        internal bool Running { get; set; }
        internal bool StartResult { get; set; } = true;
        #endregion

        #region Methods
        public bool IsRunning() => Running;

        public bool TryStart()
        {
            events.Add("start-vpn");
            return StartResult;
        }
        #endregion
    }

    internal sealed class FakePreparation(List<string> events) : IPublicProfilePreparation
    {
        #region Properties and Indexers
        internal PublicSetupResult Result { get; set; } = new PublicSetupResult(true, "Prepared");
        internal Action? OnPrepare { get; set; }
        #endregion

        #region Methods
        public Task<PublicSetupResult> PrepareAsync()
        {
            events.Add("prepare");
            OnPrepare?.Invoke();
            return Task.FromResult(Result);
        }
        #endregion
    }

    internal sealed class FakeInteraction(List<string> events) : IConnectionInteraction
    {
        #region Properties and Indexers
        internal bool RouteConsent { get; set; } = true;
        internal bool PreparationConsent { get; set; } = true;
        internal bool SignInResult { get; set; } = true;
        internal Action? OnSignIn { get; set; }
        internal List<string> Statuses { get; } = [];
        internal List<(string Message, string Title, ConnectionMessageKind Kind)> Messages { get; } = [];
        #endregion

        #region Methods
        public void ReportStatus(string status) => Statuses.Add(status);

        public void ShowMessage(string message, string title, ConnectionMessageKind kind) =>
            Messages.Add((message, title, kind));

        public bool ConfirmRoute(TargetDefinition target, ServerProfile destination, ServerProfile current)
        {
            events.Add("confirm-route");
            return RouteConsent;
        }

        public bool ConfirmPublicPreparation()
        {
            events.Add("confirm-prepare");
            return PreparationConsent;
        }

        public bool SignIn(string path)
        {
            events.Add("sign-in");
            OnSignIn?.Invoke();
            return SignInResult;
        }
        #endregion
    }
}
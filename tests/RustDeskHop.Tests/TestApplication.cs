namespace RustDeskHop.Tests
{
    // UI tests use the real controls, but never discover installed software, read
    // personal configuration, launch RustDesk or request administrator approval.
    internal static class TestApplication
    {
        #region Methods
        internal static MainForm CreateMainForm(AppSettings? settings = null)
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            return new MainForm(new MemorySettingsStore(settings ?? new AppSettings()),
                                rig.Workflow,
                                _ => rig.Interaction,
                                settings
                               );
        }

        internal static PublicSignInForm CreateSignInForm(string path)
        {
            FakeRustDesk client = new FakeRustDesk([]);
            return new PublicSignInForm(path, client, client);
        }
        #endregion
    }

    internal sealed class MemorySettingsStore(AppSettings settings) : ISettingsStore
    {
        #region Methods
        public AppSettings Load(out string? warning)
        {
            warning = null;
            return settings;
        }

        public void Save(AppSettings updated) => settings = updated;
        #endregion
    }
}
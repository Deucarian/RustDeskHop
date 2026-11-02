using RustDeskHop.Connections;

namespace RustDeskHop.Integrations.RustDesk
{
    internal sealed class PublicProfilePreparation : IPublicProfilePreparation
    {
        #region Methods
        // The sign-in workflow must obtain explicit consent before entering this adapter.
        public async Task<PublicSetupResult> PrepareAsync()
        {
            RustDeskSessions.CloseVisibleWindows();
            await Task.Delay(750);
            return await RustDeskPublicProfileSetup.RequestAsync();
        }
        #endregion
    }
}
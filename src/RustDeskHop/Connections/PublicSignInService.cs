namespace RustDeskHop.Connections
{
    internal sealed class PublicSignInService(IRustDeskState state, IPublicProfilePreparation preparation)
        : IPublicSignIn
    {
        #region Methods
        public async Task<bool> EnsureReadyAsync(string rustDeskPath, IConnectionInteraction interaction)
        {
            RustDeskDefaultRoute route = state.ReadDefaultRoute();
            if (route == RustDeskDefaultRoute.UNKNOWN)
            {
                interaction
                    .ShowMessage("RustDeskHop cannot read RustDesk's default network. Open RustDesk and check its network settings, then retry. No settings or sessions were changed.",
                                 "Check RustDesk's network",
                                 ConnectionMessageKind.WARNING
                                );
                interaction.ReportStatus("Connection paused — RustDesk's default network is unknown.");
                return false;
            }

            if (route == RustDeskDefaultRoute.PUBLIC && state.HasLoginToken())
                return true;

            if (route == RustDeskDefaultRoute.PRIVATE)
            {
                if (!interaction.ConfirmPublicPreparation())
                {
                    interaction.ReportStatus("Public sign-in setup cancelled.");
                    return false;
                }

                interaction.ReportStatus("Preparing RustDesk's public sign-in…");
                PublicSetupResult setup = await preparation.PrepareAsync();
                if (!setup.Success)
                {
                    interaction.ShowMessage(setup.Message, "Public sign-in setup failed", ConnectionMessageKind.ERROR);
                    interaction.ReportStatus("Public sign-in setup failed.");
                    return false;
                }
            }

            if (!interaction.SignIn(rustDeskPath))
            {
                interaction.ReportStatus("Waiting for RustDesk public sign-in.");
                return false;
            }

            interaction.ReportStatus("Retrying the public connection with RustDesk's account…");
            return true;
        }
        #endregion
    }
}
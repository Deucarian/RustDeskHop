using RustDeskHop.Connections;
using RustDeskHop.Integrations.Networking;
using RustDeskHop.Integrations.RustDesk;
using RustDeskHop.Integrations.Tailscale;
using RustDeskHop.Models;
using RustDeskHop.Settings;
using RustDeskHop.UI;

namespace RustDeskHop
{
    // The only production wiring point. No container, service locator or mutable globals.
    internal static class ApplicationComposition
    {
        #region Methods
        internal static MainForm CreateMainForm(AppSettings? initialSettings = null)
        {
            InstalledRustDesk rustDesk = new InstalledRustDesk();
            PrivateNetworkAccess privateNetwork =
                new PrivateNetworkAccess(new TcpNetworkProbe(), new TailscaleClient(), Task.Delay);
            PublicSignInService publicSignIn = new PublicSignInService(rustDesk, new PublicProfilePreparation());
            ConnectionCoordinator connections =
                new ConnectionCoordinator(rustDesk, rustDesk, privateNetwork, publicSignIn);
            return new MainForm(new JsonSettingsStore(),
                                connections,
                                owner => new WinFormsConnectionInteraction(owner,
                                                                           path => new PublicSignInForm(path,
                                                                                    rustDesk,
                                                                                    rustDesk
                                                                               )
                                                                          ),
                                initialSettings
                               );
        }
        #endregion
    }
}
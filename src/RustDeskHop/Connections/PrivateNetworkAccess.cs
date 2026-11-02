using RustDeskHop.Models;

namespace RustDeskHop.Connections
{
    internal sealed class PrivateNetworkAccess(INetworkProbe probe, IVpnClient vpn, Func<TimeSpan, Task> delay)
        : IPrivateNetworkAccess
    {
        #region Methods
        public async Task<bool> EnsureAvailableAsync(ServerProfile profile, IConnectionInteraction interaction)
        {
            interaction.ReportStatus($"Checking access to {profile.Name}…");
            bool reachable = await probe.CanReachAsync(profile);
            if (!reachable && !vpn.IsRunning() && vpn.TryStart())
            {
                interaction.ReportStatus("Starting Tailscale and retrying…");
                await delay(TimeSpan.FromSeconds(2));
                reachable = await probe.CanReachAsync(profile);
            }

            if (reachable)
                return true;

            interaction.ReportStatus($"{profile.Name} is not reachable.");
            string explanation = vpn.IsRunning()
                ? "Tailscale is running, but the private RustDesk server did not answer. The server device may be offline or disconnected from Tailscale."
                : "Tailscale is not running. Open and connect Tailscale, then try again.";
            interaction.ShowMessage($"{profile.Name} is not reachable.\n\n{explanation}\n\nRustDesk was not launched.",
                                    "Private network unavailable",
                                    ConnectionMessageKind.WARNING
                                   );
            return false;
        }
        #endregion
    }
}
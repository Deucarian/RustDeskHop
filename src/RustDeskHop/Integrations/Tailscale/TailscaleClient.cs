using RustDeskHop.Connections;

namespace RustDeskHop.Integrations.Tailscale
{
    internal sealed class TailscaleClient : IVpnClient
    {
        #region Methods
        public bool IsRunning() => TailscaleState.IsRunning();
        public bool TryStart() => TailscaleState.TryStart();
        #endregion
    }
}
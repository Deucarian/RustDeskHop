using RustDeskHop.Connections;
using RustDeskHop.Models;

namespace RustDeskHop.Integrations.Networking
{
    internal sealed class TcpNetworkProbe : INetworkProbe
    {
        #region Methods
        public Task<bool> CanReachAsync(ServerProfile profile) => NetworkProbe.CanReachAsync(profile);
        #endregion
    }
}
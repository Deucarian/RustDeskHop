using RustDeskHop.Models;

namespace RustDeskHop.Connections
{
    internal interface INetworkProbe
    {
        Task<bool> CanReachAsync(ServerProfile profile);
    }
}
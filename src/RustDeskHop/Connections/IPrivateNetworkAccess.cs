using RustDeskHop.Models;

namespace RustDeskHop.Connections
{
    internal interface IPrivateNetworkAccess
    {
        Task<bool> EnsureAvailableAsync(ServerProfile profile, IConnectionInteraction interaction);
    }
}
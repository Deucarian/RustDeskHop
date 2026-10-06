using System.Net.Sockets;
using RustDeskHop.Connections;
using RustDeskHop.Models;

namespace RustDeskHop.Integrations.Networking
{
    internal static class NetworkProbe
    {
        #region Methods
        public static async Task<bool> CanReachAsync(ServerProfile profile,
                                                     CancellationToken cancellationToken = default)
        {
            if (!profile.RequiresPrivateNetwork)
                return true;

            try
            {
                string host = GetProbeHost(profile);

                // ProbePort remains the explicit probe setting, independent of the server's port.
                int port = profile.ProbePort > 0 ? profile.ProbePort : 21116;
                using TcpClient client = new TcpClient();
                using CancellationTokenSource timeout =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                await client.ConnectAsync(host, port, timeout.Token).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static string GetProbeHost(ServerProfile profile) =>
            ServerAddressParser.GetHost(string.IsNullOrWhiteSpace(profile.ProbeHost)
                                            ? profile.ServerAddress
                                            : profile.ProbeHost
                                       );
        #endregion
    }
}
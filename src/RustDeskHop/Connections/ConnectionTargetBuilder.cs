using RustDeskHop.Models;

namespace RustDeskHop.Connections
{
    internal static class ConnectionTargetBuilder
    {
        #region Methods
        public static string Build(TargetDefinition target, ServerProfile profile, RustDeskDefaultRoute defaultRoute)
        {
            string id = ComputerAddress.Normalize(target.RustDeskId);
            if (ComputerAddress.Validate(id) is not null)
            {
                throw new InvalidOperationException("The saved RustDesk ID is invalid.");
            }

            if (profile.IsPublic)
            {
                if (defaultRoute != RustDeskDefaultRoute.PUBLIC)
                {
                    throw new
                        InvalidOperationException("RustDesk's default network is not confirmed public. Check its network settings, then retry. No connection was opened."
                                                 );
                }

                // RustDesk 1.4.9 clears the account token for all other-server targets,
                // including @public. A bare ID preserves the public account login.
                return id;
            }

            string server = profile.ServerAddress.Trim();
            if (string.IsNullOrWhiteSpace(server) || server.IndexOfAny(['?', '&']) >= 0)
            {
                throw new InvalidOperationException("The saved RustDesk server address is invalid.");
            }

            string key = profile.PublicKey.Trim();
            if (key.IndexOfAny(['?', '&']) >= 0)
            {
                throw new InvalidOperationException("The saved RustDesk server key is invalid.");
            }

            string keyPart = string.IsNullOrWhiteSpace(key) ? "" : $"?key={key}";
            return $"{id}@{server}{keyPart}";
        }
        #endregion
    }
}
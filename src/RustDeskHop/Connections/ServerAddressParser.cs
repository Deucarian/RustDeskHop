using System.Net;
using System.Net.Sockets;

namespace RustDeskHop.Connections
{
    // One host parser for route detection and reachability. ProbePort remains a
    // separate setting; extracting a host must not silently choose the probe port.
    internal static class ServerAddressParser
    {
        #region Methods
        internal static string GetHost(string address)
        {
            address = address.Trim();
            if (address.StartsWith('['))
            {
                int closingBracket = address.IndexOf(']');
                if (closingBracket < 0)
                    throw new FormatException("The IPv6 address is missing its closing bracket.");

                string host = address[1..closingBracket];
                if (!IPAddress.TryParse(host, out IPAddress? ipv6)
                    || ipv6.AddressFamily != AddressFamily.InterNetworkV6)
                    throw new FormatException("The bracketed address is not IPv6.");

                string suffix = address[(closingBracket + 1)..];
                if (suffix.Length > 0 && (!suffix.StartsWith(':') || !ValidPort(suffix[1..])))
                    throw new FormatException("The server port is invalid.");

                return host;
            }

            // Unbracketed IPv6 is a complete address, not a hostname followed by a port.
            if (IPAddress.TryParse(address, out _))
                return address;

            int colon = address.IndexOf(':');
            if (colon >= 0)
            {
                if (!ValidPort(address[(colon + 1)..]))
                    throw new FormatException("The server port is invalid.");

                address = address[..colon];
            }

            if (address.Length == 0
                || address.Any(char.IsWhiteSpace)
                || address.IndexOfAny(['/', '\\', '?', '#', '@', '[', ']']) >= 0)
                throw new FormatException("The probe host is invalid.");

            return address;
        }

        private static bool ValidPort(string value) => int.TryParse(value, out int port) && port is >= 1 and <= 65535;
        #endregion
    }
}
using System.Net;
using System.Net.Sockets;

namespace Simultria.RustDeskCompanion;

internal static class NetworkProbe
{
    internal static string GetProbeHost(ServerProfile profile)
    {
        var address = (string.IsNullOrWhiteSpace(profile.ProbeHost)
            ? profile.ServerAddress : profile.ProbeHost).Trim();

        if (address.StartsWith('['))
        {
            var closingBracket = address.IndexOf(']');
            if (closingBracket < 0) throw new FormatException("The IPv6 address is missing its closing bracket.");
            var host = address[1..closingBracket];
            if (!IPAddress.TryParse(host, out var ipv6) || ipv6.AddressFamily != AddressFamily.InterNetworkV6)
                throw new FormatException("The bracketed address is not IPv6.");
            var suffix = address[(closingBracket + 1)..];
            if (suffix.Length > 0 && (!suffix.StartsWith(':') || !ValidPort(suffix[1..])))
                throw new FormatException("The server port is invalid.");
            return host;
        }

        // Unbracketed IPv6 is a complete address, not a hostname followed by a port.
        if (IPAddress.TryParse(address, out _)) return address;
        var colon = address.IndexOf(':');
        if (colon >= 0)
        {
            if (!ValidPort(address[(colon + 1)..])) throw new FormatException("The server port is invalid.");
            address = address[..colon];
        }
        if (address.Length == 0 || address.Any(char.IsWhiteSpace) || address.IndexOfAny(['/', '\\', '?', '#', '@', '[', ']']) >= 0)
            throw new FormatException("The probe host is invalid.");
        return address;
    }

    private static bool ValidPort(string value) => int.TryParse(value, out var port) && port is >= 1 and <= 65535;

    public static async Task<bool> CanReachAsync(ServerProfile profile, CancellationToken cancellationToken = default)
    {
        if (!profile.RequiresPrivateNetwork) return true;

        try
        {
            var host = GetProbeHost(profile);
            // ProbePort remains the explicit probe setting, independent of the server's port.
            var port = profile.ProbePort > 0 ? profile.ProbePort : 21116;
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(host, port, timeout.Token).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

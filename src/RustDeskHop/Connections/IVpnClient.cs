namespace RustDeskHop.Connections
{
    // Starting an installed client does not imply VPN authentication or reachability.
    internal interface IVpnClient
    {
        bool IsRunning();
        bool TryStart();
    }
}
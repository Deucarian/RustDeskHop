using RustDeskHop.Models;

namespace RustDeskHop.Connections
{
    internal interface IRustDeskState
    {
        RustDeskDefaultRoute ReadDefaultRoute();
        ServerProfile? DetectDefaultProfile(AppSettings settings);
        bool HasLoginToken();
        string? ReadLoginFingerprint();
    }
}
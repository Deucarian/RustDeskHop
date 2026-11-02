using RustDeskHop.Models;

namespace RustDeskHop.Connections
{
    internal enum ConnectionMessageKind
    {
        INFORMATION,
        WARNING,
        ERROR
    }

    // The workflow asks for decisions; only the UI owns dialogs and presentation.
    internal interface IConnectionInteraction
    {
        void ReportStatus(string status);
        void ShowMessage(string message, string title, ConnectionMessageKind kind);
        bool ConfirmRoute(TargetDefinition target, ServerProfile destination, ServerProfile current);
        bool ConfirmPublicPreparation();
        bool SignIn(string rustDeskPath);
    }
}
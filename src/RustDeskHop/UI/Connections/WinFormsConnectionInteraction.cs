using RustDeskHop.Connections;
using RustDeskHop.Models;

namespace RustDeskHop.UI
{
    internal sealed class WinFormsConnectionInteraction(IWin32Window owner, Func<string, Form> createSignInForm)
        : IConnectionInteraction
    {
        #region Methods
        public void ReportStatus(string status)
        {
            // Opening RustDesk is not confirmation of a connected remote session.
            // The row shows launch progress; RustDesk owns the remote session UI.
        }

        public void ShowMessage(string message, string title, ConnectionMessageKind kind) => MessageBox.Show(owner,
                 message,
                 title,
                 MessageBoxButtons.OK,
                 kind switch
                 {
                     ConnectionMessageKind.ERROR => MessageBoxIcon.Error,
                     ConnectionMessageKind.WARNING => MessageBoxIcon.Warning,
                     _ => MessageBoxIcon.Information,
                 }
            );

        public bool ConfirmRoute(TargetDefinition target, ServerProfile destination, ServerProfile current) =>
            MessageBox.Show(owner,
                            $"{target.Name} uses “{destination.Name}”, while RustDesk's default is “{current.Name}”.\n\nRustDeskHop will route only this new connection through “{destination.Name}”. Existing sessions stay open.\n\nConnect now?",
                            "Route through another RustDesk network?",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                           )
            == DialogResult.Yes;

        public bool ConfirmPublicPreparation() => MessageBox.Show(owner,
                                                                  "Public connections need RustDesk's public default to use your account login. RustDeskHop can prepare it; saved private clients will keep their own explicit routes.\n\nThis changes this PC's incoming/default registration. Visible RustDesk sessions will close, and Windows may ask for administrator approval. Do not continue if private RustDesk access is your only way into this PC. Continue?",
                                                                  "Prepare public RustDesk sign-in?",
                                                                  MessageBoxButtons.YesNo,
                                                                  MessageBoxIcon.Question
                                                                 )
                                                  == DialogResult.Yes;

        public bool SignIn(string rustDeskPath)
        {
            using Form form = createSignInForm(rustDeskPath);
            return form.ShowDialog(owner) == DialogResult.OK;
        }
        #endregion
    }
}
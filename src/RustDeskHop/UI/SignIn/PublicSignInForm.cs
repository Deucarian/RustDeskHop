using RustDeskHop.Connections;
using RustDeskHop.UI.Controls;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI
{
    internal sealed class PublicSignInForm : BrandedForm
    {
        #region Constants and Fields
        private readonly string _rustDeskPath;
        private readonly IRustDeskClient _client;
        private readonly Label _statusLabel = new Label();
        private readonly System.Windows.Forms.Timer _loginTimer = new System.Windows.Forms.Timer()
        {
            Interval = 750
        };
        private readonly RustDeskSignInAttempt _signInAttempt;
        #endregion

        #region Constructors and Destructors
        public PublicSignInForm(string rustDeskPath, IRustDeskClient client, IRustDeskState state)
        {
            _rustDeskPath = rustDeskPath;
            _client = client;
            _signInAttempt = new RustDeskSignInAttempt(state.ReadLoginFingerprint);
            Text = "One-time RustDesk public sign-in";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(580, 340);
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UiMetrics.PAGE_INSET),
                ColumnCount = 1,
                RowCount = 5,
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label
                                {
                                    Text = "Finish signing in inside RustDesk", AutoSize = true, Font = AppTheme.strong,
                                    Margin = new Padding(0, 0, 0, UiMetrics.GAP),
                                }
                               );
            layout.Controls.Add(new Label
                                {
                                    Text =
                                        "In RustDesk, open Settings → Account → Login and complete the browser sign-in. A new saved login will resume this connection. If you have already finished, choose Retry connection. RustDesk checks whether the account is accepted.",
                                    AutoSize = true, MaximumSize = new Size(520, 0),
                                    Margin = new Padding(0, 0, 0, UiMetrics.INSET),
                                }
                               );
            _statusLabel.Text = "Waiting for RustDesk sign-in…";
            _statusLabel.AutoSize = true;
            _statusLabel.MaximumSize = new Size(520, 0);
            _statusLabel.ForeColor = AppTheme.muted;
            layout.Controls.Add(_statusLabel);
            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
            };
            ModernButton openRustDesk = new ModernButton
            {
                Text = "Open RustDesk",
                Primary = true,
                AutoSize = true
            };
            openRustDesk.Click += (_, _) => OpenRustDesk();
            buttons.Controls.Add(openRustDesk);
            ModernButton checkAgain = new ModernButton
            {
                Text = "Retry connection",
                AutoSize = true
            };
            checkAgain.Click += (_, _) => CheckLogin(userRequestedRetry: true);
            buttons.Controls.Add(checkAgain);
            ModernButton cancel = new ModernButton
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            buttons.Controls.Add(cancel);
            layout.Controls.Add(buttons, 0, 4);
            WindowContent.Controls.Add(layout);
            CancelButton = cancel;
            _loginTimer.Tick += (_, _) => CheckLogin();
            Shown += (_, _) =>
            {
                OpenRustDesk();
                _loginTimer.Start();
            };
            FormClosed += (_, _) => _loginTimer.Stop();
        }
        #endregion

        #region Methods
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _loginTimer.Dispose();
            base.Dispose(disposing);
        }

        private void OpenRustDesk()
        {
            try
            {
                _client.Open(_rustDeskPath);
                _statusLabel.Text = "RustDesk is open. Waiting for sign-in…";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"RustDesk could not be opened: {ex.Message}";
            }
        }

        private void CheckLogin(bool userRequestedRetry = false)
        {
            if (!_signInAttempt.CanContinue(userRequestedRetry))
            {
                _statusLabel.Text = "Waiting for a new saved login. Finished? Choose Retry connection.";
                return;
            }

            _loginTimer.Stop();
            _statusLabel.Text = "Retrying connection…";
            DialogResult = DialogResult.OK;
            Close();
        }
        #endregion
    }
}
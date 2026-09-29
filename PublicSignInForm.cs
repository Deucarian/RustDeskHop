namespace Simultria.RustDeskCompanion;

internal sealed class PublicSignInForm : BrandedForm
{
    private readonly string rustDeskPath;
    private readonly Label statusLabel = new();
    private readonly System.Windows.Forms.Timer loginTimer = new() { Interval = 750 };
    private readonly RustDeskSignInAttempt signInAttempt = new(() => RustDeskAccountState.ReadLoginFingerprint());

    public PublicSignInForm(string rustDeskPath)
    {
        this.rustDeskPath = rustDeskPath;

        Text = "One-time RustDesk public sign-in";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(580, 340);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
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
            Text = "Finish signing in inside RustDesk",
            AutoSize = true,
            Font = AppTheme.Strong,
            Margin = new Padding(0, 0, 0, 10),
        });

        layout.Controls.Add(new Label
        {
            Text = "In RustDesk, open Settings → Account → Login and complete the browser sign-in. A new saved login will resume this connection. If you have already finished, choose Retry connection. RustDesk checks whether the account is accepted.",
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Margin = new Padding(0, 0, 0, 14),
        });

        statusLabel.Text = "Waiting for RustDesk sign-in…";
        statusLabel.AutoSize = true;
        statusLabel.MaximumSize = new Size(520, 0);
        statusLabel.ForeColor = AppTheme.Muted;
        layout.Controls.Add(statusLabel);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };

        var openRustDesk = new ModernButton { Text = "Open RustDesk", Primary = true, AutoSize = true };
        openRustDesk.Click += (_, _) => OpenRustDesk();
        buttons.Controls.Add(openRustDesk);

        var checkAgain = new ModernButton { Text = "Retry connection", AutoSize = true };
        checkAgain.Click += (_, _) => CheckLogin(userRequestedRetry: true);
        buttons.Controls.Add(checkAgain);

        var cancel = new ModernButton { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(cancel);

        layout.Controls.Add(buttons, 0, 4);
        WindowContent.Controls.Add(layout);

        CancelButton = cancel;
        loginTimer.Tick += (_, _) => CheckLogin();
        Shown += (_, _) =>
        {
            OpenRustDesk();
            loginTimer.Start();
        };
        FormClosed += (_, _) => loginTimer.Stop();
    }

    private void OpenRustDesk()
    {
        try
        {
            RustDeskLauncher.Open(rustDeskPath);
            statusLabel.Text = "RustDesk is open. Waiting for sign-in…";
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"RustDesk could not be opened: {ex.Message}";
        }
    }

    private void CheckLogin(bool userRequestedRetry = false)
    {
        if (!signInAttempt.CanContinue(userRequestedRetry))
        {
            statusLabel.Text = "Waiting for a new saved login. Finished? Choose Retry connection.";
            return;
        }

        loginTimer.Stop();
        statusLabel.Text = "Retrying connection…";
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) loginTimer.Dispose();
        base.Dispose(disposing);
    }
}

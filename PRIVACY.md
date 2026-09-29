# Privacy

RustDeskHop has no analytics, advertising, crash-upload service, account system or automatic update check.

It stores computer names, RustDesk IDs, server addresses, server public keys and profile assignments locally in `%APPDATA%\SimultriaRustDeskCompanion\settings.json`. A server public key is not a private key, but server details and IDs can still be sensitive. Remote passwords and sign-in tokens are not stored by RustDeskHop.

When you request a private connection, it resolves the configured hostname and opens a TCP reachability probe to the configured server/port. DNS and the target server can observe that traffic. It then passes the requested connection target to the separately installed RustDesk process. Public sign-in checks whether RustDesk's local file contains a token; it does not upload that token. RustDesk and any browser/account provider handle their own traffic, authentication and privacy policies; see [RustDesk privacy](https://rustdesk.com/privacy/).

Only explicitly requested public-login preparation changes RustDesk configuration. It creates local recovery copies beside affected files; these may contain RustDesk secrets. Damaged companion settings also require a backup before replacement. Backups are not uploaded automatically.

GitHub receives information you submit in issues, pull requests or private security reports under [GitHub's privacy statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement). Public reports are visible to everyone. Redact screenshots and never upload entire configuration files.

Uninstalling RustDeskHop removes installed program files and installer-owned shortcuts but deliberately retains settings and backups. To remove retained data, first exit the companion and manually delete only its settings directory after deciding whether you need it. RustDesk files, credentials, services and backups are not removed by the companion's uninstaller.

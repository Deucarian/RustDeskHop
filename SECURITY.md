# Security policy

## Report privately

Please use [Report a vulnerability](https://github.com/Deucarian/RustDeskHop/security/advisories/new).
Private vulnerability reporting is enabled. Do not disclose exploitable problems or credentials in public issues.
Describe the affected version, impact and minimal reproduction using fictional configuration. Never send passwords, access tokens, private keys or another person's data.

This is a volunteer project: there is no guaranteed response time or bounty. If you accidentally publish a secret, revoke/rotate it immediately; deleting a comment does not revoke it.

## Supported versions

Report problems against the newest release, including prereleases. Older releases are not maintained independently. A fix is not shipped until a new release containing it is published; check the release notes.

## Trust boundaries

- RustDeskHop launches a separately installed RustDesk. RustDesk handles remote authentication and sessions.
- Routine routing neither requires elevation nor changes the default RustDesk registration.
- Explicit public-login preparation can change user/service configuration after confirmation, backup and UAC. Cancel it if you rely on private incoming registration. Alternate-administrator elevation fails closed.
- Local settings, server public keys and recovery backups are private operational data even when they are not passwords. Protect them with normal Windows account/file permissions.
- Backups can contain RustDesk secrets. Do not attach them to bug reports.
- An Authenticode signature or build attestation establishes provenance, not a guarantee of safety. See [code signing policy](docs/CODE_SIGNING.md).

Never disable Windows security protections to run a download. Builds must not contain developer settings or remote credentials.

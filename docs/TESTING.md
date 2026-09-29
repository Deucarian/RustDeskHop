# Public-readiness testing

Automated checks are evidence for specific behaviors, not a substitute for a human connection test or a clean consumer Windows installation.

## Automated coverage

- Production configuration parsing, validation, no-write startup, atomic save and damaged-file recovery.
- Public-profile backup preflight, partial failures, command/file rollback and rollback-failure reporting using isolated files and a fake command runner.
- Alternate/missing Windows identity rejection before elevated setup.
- DNS/IPv4/IPv6 parsing and real loopback reachability.
- Connection target construction, token/config parsing, tray lifecycle, branding and layout containment/long-label/resizing checks.
- Self-contained publish and a fail-closed check for private settings, credentials and build-only assemblies in downloads.
- Disposable GitHub-hosted Windows runner: installer, Start Menu, startup default-off/opt-in, published EXE form/resource self-check, upgrade, uninstall and retained user-owned files/settings. This is a clean CI runner, **not** a non-admin Windows 11 user/SmartScreen test or a visible desktop interaction test.

Local .NET 10 regression run on 2026-09-29: 114 tests passed; 11 release-tool checks passed; the self-contained EXE's `--verify-install` check passed. The optional documentation preview renders production controls with fictional data, not a connected session. One available physical display is 1920×1080 at 100% scaling. Real 125/150/200% or mixed-monitor validation has not been performed. Record CI run links below when available.

[Initial Windows CI evidence](https://github.com/Deucarian/RustDeskHop/actions/runs/36574951647): all 114 tests, publish/license validation, 11 release-tool checks, and installer/shortcut/upgrade/uninstall smoke checks passed on Windows Server 2025 with Inno Setup 6.7.1. Later changes must pass the same required check before merging; see the PR's latest run for its exact source revision.

## Required hands-on checks (not yet complete)

Use disposable test machines and a fallback route; never jeopardize the only connection to a remote host.

| Check | Procedure and expected result | Status |
| --- | --- | --- |
| Fresh Windows 11 x64 standard user | Download via browser, inspect publisher warning, install without elevation, open from Start Menu, opt into startup, sign out/in, upgrade and uninstall. Retain settings; no RustDesk changes. | Pending |
| Public/private round trip | Record local and remote RustDesk versions. Open public A, private B, then public A while the first session remains usable. Confirm destination identity and outgoing route. | Pending |
| Previous/current RustDesk releases | Repeat routing/sign-in/recovery on explicitly recorded versions. Do not infer compatibility from argument construction. | Pending |
| Same-account UAC | On an expendable setup, approve/cancel preparation; verify backups, correct account, expected default registration and recoverability. | Pending |
| Different-admin UAC | Approve as a different account; helper must reject with no user or service configuration change. Then manually configure RustDesk as administrator and retry normal connection. | Pending |
| 125%, 150%, 200%, mixed monitors | Minimum/default/maximized dashboard and editors; long labels, keyboard navigation, scrolling; move between differently scaled monitors. No clipping/inaccessible actions. | Pending |
| Signing | After enrollment, inspect app/setup signatures, timestamp and fresh-browser download behavior on another PC. | Pending external approval |
| Branding | Resolve permission or approved independent replacement; record evidence in BRANDING.md. | Pending maintainer/upstream decision |

Record tester, date, OS build, scale, exact app/RustDesk versions, result and redacted evidence for every row. Bugs go through the issue form; exploitable issues go through private security reporting. Only mark `manualValidationComplete` after relevant rows pass. Anyone can contribute evidence; no tester registration is required.

## Safe 30-second demo script

Use fictional computer names/IDs in screenshots. Show: open from Start Menu → choose a saved public computer → explain its assigned route → choose a private computer → show route confirmation → show both sessions remaining open on a disposable setup. Do not capture passwords, account tokens, real IDs or private hostnames. A recording of this full connection demo has not yet been produced; do not label static previews as proof of connection.

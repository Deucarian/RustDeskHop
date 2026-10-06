# Public-readiness testing

Automated checks are evidence for specific behaviors, not a substitute for a human connection test or a clean consumer Windows installation.

## Adjustable UI size regression coverage (2026-10-06)

- All 238 automated tests pass. The default is 75%, with a shared 50–150% preference. Tests cover all five representative sizes, fixed-size slider controls, legacy/out-of-range settings and saving only the preference without changing computer/network data.
- A fixed two-column layout replaces the noninteractive sidebar splitter. This removes the SplitterDistance exception seen during the experimental scaling preview. Native grid editing also tolerates intermediate narrow columns while layout settles.
- An 18-transition regression preserves an unfinished name edit and verifies final editing width, contained actions, tab selection/accessibility and save behavior. Scaling no longer reloads the selected network and discards drafts.
- Native inspection found that network-field rows retained their previous height after shrinking. Fields now report their scaled preferred height, with a regression checking all fields and the lower hint remain contained through repeated 50–150% changes.
- Retested the native settings window at 75%, 150%, 50% and back to 75%, including keyboard slider operation and tab selection. All fields and actions remained visible, the slider stayed the same size, and no exception dialog appeared. These previews use fictional in-memory data and a fake launcher.
- The self-contained `0.1.1-local.scale.20261006` build passes all 27 release-tool checks and its executable self-check. After the maintainer saved and closed the installed editor, updated/restarted the companion with a verified recoverable backup. The installed executable matches the tested package; companion settings, RustDesk configuration and both icon masters/outputs retained their hashes. All four original RustDesk processes stayed running. The installed main window was inspected at its 75% default.
- These checks do not establish a new real Windows high-DPI, mixed-monitor, clean-install or authenticated-connection result. Existing release-readiness gates remain unchanged.

## Approved grouped-row design (2026-10-06)

- Implemented the approved mockup's hierarchy: flat headerless dashboard, names over muted IDs, quiet route labels, solid-blue Connect actions and an outlined management action. Management uses a light network sidebar and quieter segmented tabs with inline computer entry. The approved icon master is unchanged.
- Native preview checks at the current display scale covered both windows, network settings, inline name/ID entry, Tab navigation, a simulated per-row test, normalized-ID saving and editing an existing name. The focused field border and footer spacing were corrected after screenshot inspection. The preview uses fictional in-memory settings and a fake launcher; it cannot modify personal configuration or initiate a remote session.
- New regression coverage checks accessible grouped identities, ID formatting, headerless lists, footer containment, short-list sizing, preserving user-resized windows, arrow-key section selection and the native editing field's containment. Existing draft, validation, connection-workflow, long-list/name and resizing tests remain part of the suite.
- All 229 tests pass, with no skipped tests. The self-contained `0.1.1-local.design.20261006` package passes its executable self-check and all 27 release-tool checks. Fictional-data dashboard and management previews are included in the README.
- After the maintainer saved and closed the installed editor, updated and restarted only RustDeskHop with a recoverable backup. The installed executable matches the checked package and the backup matches the previous executable. Saved companion settings, RustDesk configuration, master SVG and installed ICO retained their hashes; all four existing RustDesk processes remained running. Inspected the installed grouped-row dashboard after restart.
- No new high-DPI/mixed-monitor, authenticated-connection, clean-install, signing or release-readiness claim is made. GitHub publication is not part of this local design update.

## Row-local computer actions and compact footer (2026-10-06)

- All 220 tests pass. New coverage checks clicked-row targeting rather than current selection, preserving another row's edits when removing a draft, Enter/Space activation, the non-data Add row, and showing Delete network only in Network settings. Populated computer editors start at the table with no visible instruction label; the secondary management button renders blue text on white. Existing layout, long-label/list, validation and connection-workflow coverage remains green.
- Compared renders of separate and integrated footer variants using fictional data. Selected the separate bordered management button, an 8-pixel list-to-action gap and a 900-by-280 default client area (60 pixels shorter). Outer page margins remain 20 logical pixels. Connect button styling and icon artwork are unchanged.
- Each computer has Test and Remove actions. A final Add computer row creates editable fields without another window; no separate computer-action bar remains. Network deletion is confined to Network settings, with an explicit label. Drafts still require Save network.
- Native preview checks at the current display scale confirmed visible keyboard focus, Enter opening management, and the Computers tab without introductory copy. Added a fictional row, entered its name and grouped numeric ID, used Tab then Enter to start its simulated Test, observed the pending state and truthful launch-only feedback, and saved the normalized ID. The preview uses in-memory fictional settings and a simulated connection workflow; no authenticated remote connection, network discovery or new high-DPI result is claimed.
- The self-contained `0.1.1-local.rows.20261006` build passes its executable self-check and all 27 release-tool checks. Updated and restarted the existing local installation with a recoverable backup. Installed/package executable hashes match; the backup matches the previous build. Companion settings, RustDesk configuration, the master SVG and generated icon are unchanged, and all four existing RustDesk processes stayed running. Observed the installed main window and row-based Computers tab after restart; no GitHub publication was performed.

## Inline setup and title-free layout (2026-10-06)

- 212 tests pass, including row insertion without a dialog, repeated Add focusing an incomplete row, editable new/read-only saved IDs, atomic save, draft discard, duplicate and malformed-ID feedback, and numeric-ID normalization.
- Test connection uses unsaved computer/network snapshots and the management window as dialog owner. Tests verify no settings writes, no duplicate launch while pending, blocked editor close/navigation, restoration after every outcome and truthful launch-only feedback. Invalid IDs are rejected before public preparation/login side effects.
- The dashboard has no content title, uses solid-blue row actions, has a roomier default size and places Manage computers & networks below the list. Page margins and field/action gaps use shared tokens. Existing minimum/default/large-window, long-list and containment checks pass.
- Native UI inspection used an isolated in-memory preview with fictional computers and a simulated launcher. Entered a computer name directly in the added row, used Tab to enter a grouped numeric ID, checked the disabled Opening state and truthful launch-only feedback, then saved and confirmed the normalized ID and correct network on the dashboard. No Add computer popup opened. This is not a successful authenticated connection test and does not modify personal settings.
- The self-contained build `0.1.1-local.inline.20261006` passes its executable self-check and all 27 release-tool checks. The dashboard image was regenerated from fictional data. Current icon artwork is unchanged.
- With the user's restart confirmation, deployed this build to the existing local installation and reopened it. The installed executable matches the checked build, the previous executable matches its recoverable backup, and the icon hash is unchanged. Saved companion settings and RustDesk configuration hashes were unchanged during deployment; all four existing RustDesk processes remained running. The updated management window was observed after restart and left open for the user. No GitHub publication was performed.
- README setup steps and the fictional-data dashboard image describe inline adding and the limits of Test connection. Real high-DPI/mixed-monitor, clean-install and remote-authentication checks remain separate.

## Compact interface validation (2026-10-06)

- 191 automated tests pass. The updated dashboard is checked at 680×300, 740×480, default and larger sizes, with repeated resizing, long labels and scrolling. Both network-management tabs are checked for contained controls, including 60-computer lists.
- Row-button clicks, Enter and Space delegate to the corresponding computer, not a stale selection. Pending launches reject duplicate requests and restore controls after started, cancelled, blocked and failed outcomes.
- Draft-model tests cover scoped add/rename/remove, duplicate-ID rejection within a network (the same ID in different networks is allowed), atomic validation, reload/discard and preserving unrelated computers. Existing persistence and label-editing tests still pass.
- Used native computer automation to inspect the real main window, row-specific Opening… state, network settings, Computers tab and Add computer dialog in an isolated in-memory preview. No personal configuration or RustDesk process is used by that preview.
- In that preview, added a fictional computer to a private network using Name → Tab → RustDesk ID → Enter, saved the network and confirmed the new row retained its private route on the main screen.
- The self-contained Windows build passes its executable self-check and all 27 release-tool checks. Generated icon and SVG hashes match the installed version exactly. README instructions and its fictional-data dashboard image describe this revision.
- These checks use the current 100% display scale. They do not establish new high-DPI/mixed-monitor, clean-install or authenticated remote-connection results. Previous release-readiness limitations remain open.
- With the user's approval, updated and restarted only the installed companion as `0.1.1-local.ux.20261006`, keeping a recoverable backup. Its executable matches the checked build; companion settings and RustDesk configuration hashes stayed unchanged, and all four existing RustDesk processes remained running. No GitHub publication is part of this local deployment.

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

The optional-signing policy update expands the release-tool suite to 27 checks (passed locally on 2026-09-29). Isolated fixtures verify that stable releases can be unsigned when both evidence gates pass, that signing never substitutes for either gate, and that absent, malformed or non-boolean evidence is rejected. The real readiness flags are not modified by these tests.

## Computer-label editing validation (2026-09-30)

- 142 .NET tests and 27 release-tool checks passed locally. Added coverage for network-scoped label editing, Unicode/trimmed names, unchanged IDs and routes, persistence through ConfigStore, empty-name rejection without partial updates, discarded unsaved edits, empty networks and 60 saved computers.
- The new editor is contained at 780×530, 900×580 and 1920×1040 in automated layout tests. Existing network-field containment and unchanged dashboard-control tests also pass.
- Inspected both Manage networks sections on the live Windows desktop at the existing 100% scale. Edited a saved private computer's label, tabbed to the read-only ID, saved using the existing Save network button and closed the dialog. The dashboard showed the updated name. A before/after comparison confirmed that only that label changed; IDs, profile assignments and network definitions were preserved. Existing RustDesk processes remained running.
- No additional high-DPI/mixed-monitor or authenticated connection checks are claimed for this label-only change. The readiness gates remain unchanged.

## Local structural-refactor evidence (2026-09-30)

The baseline suite passed all 142 tests before the refactor. The composed application passes 184 tests: existing routing, recovery, persistence, branding and layout coverage plus 42 cases for the real connection workflow with fake side effects, shared address parsing and main-window workflow delegation. Cases cover consent refusal, unknown defaults, failed preparation, default changes during sign-in, VPN retries, missing clients, launch failures and duplicate-request prevention.

The fictional-data dashboard PNG and generated ICO are each byte-identical to their baseline counterparts. This is rendering regression evidence, not a live authenticated connection test. The self-contained publish layout and executable `--verify-install` check passed, as did all 27 release-tool checks. The source project and build/release paths now use `src/RustDeskHop/RustDeskHop.csproj`.

The installed app, user settings and live RustDesk sessions were not updated or restarted. No new installer build/install, UAC, remote authentication, cross-version or high-DPI pass is claimed; an Inno Setup compiler was not available in the checked local locations. The existing readiness flags remain unchanged. This is local validation, not a new published release.

## Local convention-migration evidence (2026-09-30)

The portable personal conventions were applied to 80 owned C# files, including the app, tests and branding utility. The shared `.editorconfig` and `RustDeskHop.slnx.DotSettings` contain no machine paths, Unity requirements or project-specific logging analyzers. No formatter package, build-time formatting or on-save hook was added.

- All 184 .NET tests passed after the migration, with no skipped tests. The self-contained Windows publish, published EXE `--verify-install` self-check and all 27 release-tool checks passed.
- The seven previously approved wrapping fixtures passed against this repository's settings: 119/120-character calls stay compact, 121-character calls wrap, nested calls align and close on separate lines, comments and formatter-off regions survive, and a second formatting pass is unchanged.
- All 80 C# files are stable on repeat formatting. Syntax/naming checks passed; string-literal values and enum member order/values were preserved. The 120-character width remains a target, with long indivisible literals allowed.
- The fictional-data dashboard PNG and generated ICO each have exactly the same SHA-256 hash as the pre-migration files. JSON names, the saved-settings location and installer identity remain unchanged.

These are local checks, not a release or a new hands-on remote-connection/DPI/installer pass. HoloHelmet checkouts, installed applications, live sessions, saved RustDesk/RustDeskHop configuration and release-readiness gates were not changed by this migration.

## Fixed-size interface and motion validation (2026-10-06)

The former 90% slider setting is now the shared design density, with no slider or persisted user-scale setting. Older scale fields are ignored on load without writing the settings file. All companion forms have matching minimum/maximum sizes and disabled resizing/maximizing; lists scroll within them. Windows DPI remains independent.

- All 248 .NET tests pass locally. Coverage includes fixed sizes for the dashboard, manager and sign-in window; legacy preferences; repeated internal scaling while editing; rapid tab changes preserving drafts; hover-action targeting; animation retargeting, cancellation, disposal and idle timers; keyboard focus after inline Add; and stable computer/network row identities. Reduced motion is tested through the animation-policy seam, not by changing this laptop's Windows settings.
- Native preview checks at the laptop's existing display scale: no slider, blue-outline tabs matching Test, readable network fields, fixed dashboard dimensions after an attempted border drag, inline Add/name/ID entry, Tab navigation, simulated Test/loading/result feedback, saving a new row and adding a network with list/page transitions. The preview uses fictional data, an in-memory settings store and a fake connection launcher; it does not prove a real remote connection.
- The self-contained `0.1.1-local.motion.20261006` package passed all 27 release-tool checks and `--verify-install`. Updated/restarted only the installed companion after verifying that no editor was open. Verified the old executable backup, installed/published executable equality, unchanged saved settings, `RustDesk2.toml` and logo hashes, and the continued presence of all existing RustDesk processes. The installed dashboard was inspected again with its three existing computers, no slider and disabled maximize. This is a local build from the working tree, not a GitHub publication.
- Documentation images are rendered from production controls with fictional data. Live screenshots containing real computer identifiers are not added to the repository.
- Real 125/150/200%, mixed-monitor and small-work-area checks remain pending for this revision. No clean-user install, authentication/UAC, cross-version connection or public release is claimed. The existing release-readiness flags stay unchanged.

## Required hands-on checks (not yet complete)

Use disposable test machines and a fallback route; never jeopardize the only connection to a remote host.

"Fresh Windows" means a disposable Windows 11 VM or a clean standard-user account, not reinstalling the maintainer's laptop. This clean-user installation pass is deferred for now at the maintainer's request; do not count it as passed or run sign-out/uninstall tests against the live remote-access environment.

| Check | Procedure and expected result | Status |
| --- | --- | --- |
| Fresh Windows 11 x64 standard user | Download via browser, inspect publisher warning, install without elevation, open from Start Menu, opt into startup, sign out/in, upgrade and uninstall. Retain settings; no RustDesk changes. | Deferred for this pass |
| Public/private round trip | Record local and remote RustDesk versions. Open public A, private B, then public A while the first session remains usable. Confirm destination identity and outgoing route. | Partial: outgoing public desktop now verified after routing fix; private authentication/full round trip pending. See live results below. |
| Previous/current RustDesk releases | Repeat routing/sign-in/recovery on explicitly recorded versions. Do not infer compatibility from argument construction. | Pending |
| Same-account UAC | On an expendable setup, approve/cancel preparation; verify backups, correct account, expected default registration and recoverability. | Pending |
| Different-admin UAC | Approve as a different account; helper must reject with no user or service configuration change. Then manually configure RustDesk as administrator and retry normal connection. | Pending |
| 125%, 150%, 200%, mixed monitors | Minimum/default/maximized dashboard and editors; long labels, keyboard navigation, scrolling; move between differently scaled monitors. No clipping/inaccessible actions. | Partial: normal/maximized dashboard and networks plus Add computer inspected at 125/150%; refresh-layout defect fixed and retested. 200%, minimum-size/long-label matrix and mixed monitors remain pending. |
| Branding | Keep the approved logo and record status in BRANDING.md. Do not contact RustDesk; the maintainer cancelled outreach. | Unresolved; no permission or endorsement claimed |

Record tester, date, OS build, scale, exact app/RustDesk versions, result and redacted evidence for every row. Bugs go through the issue form; exploitable issues go through private security reporting. Only mark `manualValidationComplete` after the hands-on checks pass; branding permission is tracked separately by `brandingCleared`. Anyone can contribute evidence; no tester registration is required.

## Local live test pass — 2026-09-29

Observed through Windows UI automation and screenshots on Windows 11 Pro 10.0.26200. Tested the source at `502116b2492e8c647d3382eb31ed7f36d15b41dd` as `0.1.1-validation.20260929`, built in a separate validation directory. Local RustDesk: `1.4.9+67`; Debian test target: `1.4.8` on Debian 12. Documentation-only working changes were present; application code was unchanged. The installed companion was not replaced.

- **Passed at the existing display scale:** dashboard/default and maximized layout, network editor/default and maximized layout, Add computer layout and cancellation. Visible labels and actions were readable. Closing the companion hid its window without terminating it; relaunch restored the same process rather than creating another instance. The attempted resize did not establish a minimum-size test, so no new minimum-size result is claimed.
- **Private route partially verified:** the saved Debian server answered, the companion showed the public-to-private confirmation, and RustDesk reported a direct encrypted connection before requesting the remote password. Authentication was left to the user. A remote desktop and authenticated round trip have not been verified. The other saved Ubuntu private server did not answer the bounded TCP probe; this alone does not prove the machine is powered off.
- **Outgoing public connection failed:** after the private attempt, selecting the saved public computer launched RustDesk, which rejected the connection and requested public-account login. A nonempty cached access token exists. `RustDeskAccountState.HasLoginToken` checks presence only, and `EnsurePublicLoginAsync` treats that as sufficient to proceed. This exposes a missing recovery path when RustDesk rejects authentication; the underlying reason for rejection (including token validity versus per-route handling) is not yet determined. Do not describe the outgoing public/private round trip as working on this setup.
- **Existing incoming access preserved:** the original incoming home-desktop session panel still reported Connected after both attempts. It is an incoming session, not evidence of a successful new outgoing public connection. Its process was not stopped or restarted.
- **Configuration preserved:** saved companion settings and `RustDesk2.toml` have the same SHA-256 hashes before and after testing. RustDesk's separate `RustDesk_local.toml` changed during the connection attempts; it was not overwritten or restored, and no claim is made that all RustDesk state was unchanged.
- **DPI still pending:** permission was given to test and restore 125/150/200%, but Windows Settings did not expose a controllable window. No display setting was changed. The existing AppliedDPI value was 96; this is not high-DPI or mixed-monitor validation. The user was asked to open System → Display for a follow-up.
- **Not attempted:** sign-out, installation/uninstallation on the live laptop, UAC preparation, authentication automation, or replacing the installed RustDesk version. The installed companion was restarted from its original path and returned to its original tray state after the test build was stopped. RustDesk connection/authentication windows were left for the user.

No readiness flag has been marked complete. The public authentication rejection was investigated and resolved in the follow-up below; completing the remaining tests still requires the stated user handoffs or a disposable test environment.

## Public routing fix and live retest — 2026-09-29

The earlier rejection was caused by the outgoing target format, not demonstrated token expiry. In [RustDesk 1.4.9's connection code](https://github.com/rustdesk/rustdesk/blob/1.4.9/src/client.rs), the other-server branch replaces the account token with an empty string, including for `@public`. RustDeskHop now uses a bare ID only after confirming the effective default is public. Private targets retain their explicit server/key route. Missing/unreadable default configuration blocks public launch; private-to-public preparation still requires explicit consent and is not a routine route switch. The default is checked again immediately before launch. Fresh sign-in dialogs observe a changed token fingerprint; unchanged cached credentials cannot automatically complete them. Explicit retry remains available without claiming server validation.

- **Automated:** all 134 .NET tests passed, including public/private/unknown route guards, public-host classification, missing/locked/malformed configuration, cached-login change detection and retry/cancel action containment. All 27 release-tool checks and the self-contained EXE's `--verify-install` check passed. No actual account login, UAC or global network mutation was performed by these tests.
- **Installed local build:** `0.1.1-local.publiclogin.20260929`, built from the working changes on `bugfix/public-login-routing` above base `502116b2492e8c647d3382eb31ed7f36d15b41dd`. This is a local validation build, not a published release. The previous installed companion and matching shortcuts were backed up before replacement. The existing logo and GPL license are unchanged.
- **Outgoing public desktop passed:** clicking the saved public home computer in the updated installed companion opened an authenticated RustDesk remote desktop with the expected destination identity and visible remote screen. It did not show the previous public-account login rejection. No authentication dialog was operated and no input was sent to the remote desktop.
- **Existing access preserved:** the original incoming session still displayed Connected after the successful outgoing attempt. Its process and RustDesk's other processes were not stopped or restarted. Its status window was restored to its previous minimized state after inspection.
- **Settings preserved:** saved companion settings and `RustDesk2.toml` retained their pre-test SHA-256 hashes. No claim is made that RustDesk's transient local/session state remained unchanged.
- **Still pending:** authenticated private/public round trip, real high-DPI/mixed-monitor checks, clean-user installation and the other manual rows above. The changed sign-in dialog was covered by automated layout/state checks; the successful live connection reused RustDesk's existing account rather than exercising a fresh browser login. No readiness flag was changed.

## Display-scaling pass and refresh-layout correction — 2026-09-29

Windows Settings was made available by the maintainer. On this laptop's 1920×1080 display, the original setting was 100%. Temporarily applied 125%, then 150%, and restored 100%. After installing the layout correction, repeated the failing workflow at 150% and restored 100% again; Settings visibly confirmed the restored value. The normal scaling menu exposed 100%, 125%, 150% and 175%, not 200%. Custom scaling, sign-out and resolution changes were not attempted.

- **125%:** inspected normal/maximized dashboard, normal/maximized network editor and Add computer. Labels/actions were readable; Tab moved from Name to RustDesk ID, and Escape cancelled the Add dialog. The attempted dashboard resize did not establish a minimum-size result.
- **150%:** the same normal/maximized surfaces and Add computer were readable, but returning from network management collapsed the grid to one visible row despite space for all three. This is a binding/layout defect, not proof of a DPI-only bug: a regression test reproduced it at 100%, with a 208-pixel content height incorrectly displayed in a 100-pixel grid.
- **Correction:** invalidate the cached content-height notification after rebinding, then measure through the existing deferred sizing path. No design or routing change. The previously failing regression now passes, as do all 135 .NET tests and 27 release-tool checks.
- **Live retest:** installed `0.1.1-local.layout.20260929` with a recoverable backup, restarted only the companion, switched to 150%, opened and closed Manage networks, and confirmed all three rows remained visible. Returned to 100%. Companion settings and `RustDesk2.toml` retained their original hashes throughout.
- **Public/private status:** the public desktop opened again before the private attempt. The private route established an encrypted transport and requested its password while the public tab remained present. Authentication was left to the maintainer; no authenticated private/public round trip is claimed.
- **Still pending:** 200%, mixed monitors, confirmed minimum-size/long-label/scrolling coverage at each DPI, fresh-login/UAC/cross-version checks and a clean standard-user installation. Keep both readiness flags false. Screenshots containing real machine/account identifiers are not included in the public repository.

## Optional signing follow-up

Signing enrollment is optional and does not block `manualValidationComplete` or an otherwise validated stable release. If signing is enabled, inspect app/setup signatures and timestamps and test fresh-browser download behavior on another PC; the release workflow must reject missing or invalid signatures. This has not been performed because no provider enrollment is approved. For unsigned releases, the Windows download/warning test above remains required. See [the signing policy](CODE_SIGNING.md).

## Safe 30-second demo script

Use fictional computer names/IDs in screenshots. Show: open from Start Menu → choose a saved public computer → explain its assigned route → choose a private computer → show route confirmation → show both sessions remaining open on a disposable setup. Do not capture passwords, account tokens, real IDs or private hostnames. A recording of this full connection demo has not yet been produced; do not label static previews as proof of connection.

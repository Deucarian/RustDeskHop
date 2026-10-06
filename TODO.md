# TODO

## Fixed 90% size and consistent motion (2026-10-06)

- [x] Remove the experimental slider and saved scale preference; use the former 90% size as the shared design density without altering existing settings on startup.
- [x] Match management tabs to the blue-outline Test palette, with a quiet selected fill and accessible selection state.
- [x] Fix each companion window's minimum and maximum size; disable manual resizing/maximizing and keep long lists scrollable.
- [x] Share hover/focus/press feedback across buttons, row actions, editable fields and network items; animate list reflows and management-page changes.
- [x] Respect Windows reduced motion/high contrast, stop idle timers, dispose animation resources and preserve drafts through rapid page changes.
- [x] Pass 248 automated tests, including animation lifecycle, inline-editor focus, network-list identity and fixed-size regression coverage.
- [x] Inspect native entry, keyboard navigation, simulated Test/save, network addition and final installed dashboard; pass 27 release-tool checks and the packaged self-check, then update/restart only RustDeskHop with a verified backup and unchanged settings/logo/RustDesk processes.
- [ ] Repeat real high-DPI/mixed-monitor and small-work-area checks; the current-screen checks do not establish that matrix.
- [ ] Publish this follow-up when requested; it is separate from the prior develop promotion.

## Adjustable UI size and management tabs (2026-10-06)

- [x] Default the interface to 75%; offer a shared 50–150% slider that stays at its own normal size and remembers the chosen setting without changing Windows or RustDesk.
- [x] Keep typography, rows, fields, actions and spacing proportional; preserve unsaved edits through repeated size changes.
- [x] Fix segmented-tab selection/hover styling and keyboard focus; replace the fixed sidebar's splitter with a two-column layout to prevent the repeated SplitterDistance exception.
- [x] Pass all 238 automated tests, including 18 repeated scaling transitions while editing, preference-only persistence and shrinking network fields after growing them.
- [x] Verify native 50%, 75% and 150% layouts, pass 27 release-tool checks and the packaged self-check, then update/restart only the installed companion with a recoverable backup.
- [ ] Merge the approved UI and dependent refactor/conventions into develop after the required remote build passes; main and versioned releases remain outside this request.
- [ ] Complete the outstanding real high-DPI/mixed-monitor matrix; app zoom tests are not a substitute.

## Approved grouped-row design (2026-10-06)

- [x] Match the approved visual direction: flat white dashboard, names above muted IDs, thin dividers, quiet network labels and solid-blue Connect buttons.
- [x] Use a light sidebar and quiet segmented selector in management; keep inline Add, row-local Test/Remove, and existing save/validation behaviour.
- [x] Balance shared spacing, fit short lists, preserve manual resizing, and fix footer overlap and inline editing borders.
- [x] Preserve the approved logo, native window controls and accessible names/IDs; inspect native entry, keyboard navigation, saved rows and network settings with isolated fictional data.
- [x] Pass all 229 automated tests, 27 release-tool checks and the packaged executable self-check.
- [x] Update and restart the installed companion with a verified backup, keeping saved configuration, the icon and existing RustDesk processes unchanged.
- [ ] Finish this revision's real high-DPI/mixed-monitor pass; current-scale native checks and automated resizing do not replace it.
- [ ] Publish the local UI/refactor/conventions work only when requested.

## Quieter computer setup and clearer management action (2026-10-06)

- [x] Remove the Computers tab's persistent instructions; retain useful empty states and validation/test feedback only when needed.
- [x] Compare a separate compact footer with an integrated list-panel footer, keeping the separate white, blue-text management button. Keep solid-blue Connect buttons and the current logo.
- [x] Reduce default window height by 60 pixels and the list-to-button gap to the shared 8-pixel spacing; preserve consistent 20-pixel page margins and resizable/scrollable lists.
- [x] Put Test and Remove on each computer row and Add computer in a final list row. Keep network deletion in Network settings only, with an explicit label.
- [x] Cover clicked-row targeting, draft preservation, keyboard activation and empty-list saves alongside instruction-free layout, button rendering and compact spacing.
- [x] Pass all 220 tests, 27 release-tool checks and the executable self-check. Inspect the native row actions, then update/restart the installed companion with a backup while preserving settings, icon and RustDesk processes.
- [ ] Complete the outstanding real high-DPI/mixed-monitor matrix; current-scale checks are not a substitute.

## Inline computer setup and simpler main screen (2026-10-06)

- [x] Remove the main page title, restore solid-blue Connect buttons, move the clearer Manage computers & networks action below the list, and provide a roomier default window.
- [x] Standardize outer margins and field/action spacing through shared metrics; open management directly to Computers.
- [x] Replace the Add computer popup with a focused editable row. Validate names/IDs/duplicates inline and normalize copied numeric IDs.
- [x] Offer Test connection for saved or unsaved rows using the existing connection workflow, without saving or claiming end-to-end success.
- [x] Pass 212 tests, including inline entry, read-only saved IDs, isolated test snapshots, pending-test locking, all outcomes and validation before login/setup side effects.
- [x] Check inline name/ID entry, simulated-test feedback, saving and the resulting main-screen row in the native preview; pass the packaged self-check and all 27 release-tool checks.
- [x] Deploy and restart the installed companion after confirmation, retaining a verified backup and preserving settings, the current icon and all existing RustDesk processes.
- [ ] Repeat real high-DPI/mixed-monitor checks for this layout. Automated resizing at the current scale does not replace that matrix.

## Compact computer-first interface (2026-10-06)

- [x] Replace the dashboard subtitle, selection/action panel and routine footer with per-row Connect buttons.
- [x] Align the Computers title and Manage networks on one centerline; tighten page, list-header and row spacing.
- [x] Move Add/Remove into Manage networks → selected network → Computers. Keep edits as drafts until Save network, preassign new computers to that network, and reject duplicate IDs within it.
- [x] Keep route safety/consent checks and the existing logo; show only temporary Opening… progress, not unverified connection success.
- [x] Pass 191 automated tests, 27 release-tool checks and the packaged self-check; inspect the native windows with isolated fictional data at the current display scale.
- [x] With approval, update/restart the installed companion with a recoverable backup; verify saved settings and RustDesk configuration are unchanged and existing RustDesk processes remain running.
- [ ] Recheck the changed layout at real 125/150/200% scaling and across mixed-DPI monitors; prior DPI results are not evidence for this revision.
- [ ] Publish the UI changes and the existing local refactor/conventions work through the normal review/release process when requested.

## Personal C# conventions (2026-09-30)

- [x] Adopt portable naming, explicit types, block-scoped namespaces and member sections throughout the owned app, tests and branding utility.
- [x] Check in self-contained EditorConfig and Rider/ReSharper layout settings with the approved 120-character aligned wrapping; keep wrapping while typing disabled and add no formatter dependency or automatic formatting hooks.
- [x] Verify all 184 tests, 27 release-tool checks and the published executable self-check; reproduce the seven approved wrapping examples, confirm repeat-format stability and preserve the dashboard/icon bytes.
- [ ] Publish these conventions together with the local composition refactor through the normal review/release process when requested.

## Composed application structure (2026-09-30)

- [x] Move application code into a single `src/RustDeskHop` project with a root solution, focused models/UI/integrations/settings folders and a constructor-based composition root.
- [x] Extract connection coordination, private-network readiness and public sign-in from the main window; isolate disruptive preparation from ordinary launching.
- [x] Split elevated command handling from the backup/rollback transaction and share host parsing between route detection and probes.
- [x] Preserve the settings path/schema, installer identity and single SVG master; update build/release paths and developer instructions.
- [x] Pass all 184 tests (142 baseline plus 42 new workflow, address-consistency and UI-delegation cases). The fictional-data dashboard render is byte-identical to the baseline.
- [ ] Promote this local refactor through the normal develop/main review and release process when requested; no new release or live deployment is implied by these checks.

## Computer-label editing (2026-09-30)

- [x] Keep renaming inside Manage networks: select a network, open Computer names, edit labels, then use the existing Save network and Close controls. No new dashboard buttons or context menus.
- [x] Isolate label drafts, reject empty names, preserve IDs/routes and other networks, and cover persistence, discarded edits, empty networks and long lists. Live desktop save verified; current suite: 142 tests and 27 release-tool checks.

## Live-test findings (2026-09-29)

- [x] Fix public outgoing authentication routing: RustDesk 1.4.9 strips the account token from `@public` connections. Use a bare ID only with a confirmed public default, keep explicit private routes, and prevent an unchanged cached token from auto-completing a fresh sign-in dialog. Covered by regression tests; live results are recorded in `docs/TESTING.md`.
- [ ] Complete the authenticated Debian/public round trip after the user handles login/password prompts. Private server reachability and a password prompt are not proof of a working remote desktop.
- [x] Inspect normal/maximized dashboard and networks plus Add computer at real 125% and 150% scaling; restore the laptop's original 100%. Details and limits are in `docs/TESTING.md`.
- [x] Fix the list collapsing after returning from network management; reproduce with a failing regression test and verify the correction in the real 150% UI.
- [ ] Finish the DPI matrix: 200% (not offered by this display's normal menu), confirmed minimum-size/long-label/scrolling checks and mixed monitors. No custom scale or sign-out was attempted.
- [ ] Run clean-user installation and disruptive UAC/version checks later in a disposable environment, not on the live remote-access setup.

## Public-readiness follow-up (2026-09-29)

- [x] Upgrade the application, branding renderer and tests to .NET 10 LTS.
- [x] Reject alternate-account elevated setup before it can touch RustDesk settings.
- [x] Enable private vulnerability reporting; add public bug/feature guidance, contributing/security/privacy policies.
- [x] Exclude developer seed settings from publishing and validate distribution contents.
- [x] Add a per-user installer with Start Menu/optional startup and retained settings on uninstall; add disposable-runner smoke tests.
- [x] Preserve GPL-3.0-only, include resolved runtime notices and matching source; document installer licensing.
- [x] Prepare optional free OSS signing, signature verification and artifact provenance. Stable releases still require recorded branding/manual-validation evidence, but not signing enrollment.
- [x] Document the hobby/community project statement, free official downloads, GPL-3.0-only and possible future collaboration without automatic copyright transfer. Keep the current logo unchanged.
- [ ] Optional polish: obtain signing-provider approval, configure real credentials/policies, and verify signed app/setup on a fresh Windows PC. This is not a GPL or stable-release requirement. See `docs/CODE_SIGNING.md`.
- [ ] Branding status remains unresolved; keep the current logo. RustDesk outreach is cancelled by the maintainer: do not email or otherwise contact RustDesk, and do not ask for a sending account. The cancelled reference draft is in `docs/BRANDING.md`; no email or mailbox draft was created.
- [ ] Complete and record real clean-user, UAC, DPI/mixed-monitor and cross-version connection checks in `docs/TESTING.md`.
- [ ] Record the short real-connection demo described in `docs/TESTING.md` after those checks.
- [x] Publish the validated readiness fixes to develop/main and [v0.1.1-beta.1](https://github.com/Deucarian/RustDeskHop/releases/tag/v0.1.1-beta.1). Old assets remain unchanged; the release does not claim unperformed checks passed.

## Readiness defects found on 2026-09-29

- [x] Stop public-login preparation before any server-setting command when a configuration backup fails; cover partial setup and rollback failures with isolated tests of the production code.
- [x] Keep damaged settings unchanged at startup, warn the user, and require a successful backup before explicitly replacing them.
- [x] Validate deserialized settings, handle null/malformed lists and entries, and report save failures without crashing or applying unsaved changes.
- [x] Parse DNS, IPv4 and IPv6 probe hosts correctly and add real loopback reachability regression tests.
- [ ] Complete clean-machine, alternate-administrator/UAC, real high-DPI/mixed-monitor and cross-version connection checks before claiming broad public readiness.

## Priority: refine UI sizing and match the approved design

- [x] Restore the user-selected A / earlier thin-ring artwork as the single PNG master. Remove its outer padding without redrawing the bunny/ring, regenerate all icon sizes, and refresh only RustDeskHop's shortcut and executable shell-icon entries. This supersedes the later SVG ring experiments below.
- [x] Match RustDesk's rounded white Windows icon tile and overall mark padding, checked against the installed executable's 48px icon. Preserve the approved thinner ring and bunny, and regenerate every size from the one SVG master.
- [x] Make the ring about 18% thinner by opening its inner contour, retaining the outside diameter, original outer arcs/rounded endings and unchanged foreground bunny. Verify the generated band thickness and refresh the installed app.
- [x] Remove the remaining added transparent icon margin so the unchanged master fills Windows taskbar frames more fully. Keep this follow-up on develop, separate from the desktop-polish promotion to main.
- [x] Apply the user-approved bunny-inside-RustDesk-style composite, retaining its white background, as the single icon master including tray and shortcut assets. Keep this follow-up develop-only.
- [x] Replace the generated ring with the actual installed RustDesk SVG path and gradient, preserving exact arc endings. Enlarge the simplified bunny by about 30%, overlap it over the ring with white separation, and use one SVG master for every output.
- [ ] Verify upstream logo-use terms before public distribution of the RustDesk-style composite; user approval of the design is not trademark clearance.
- [x] Compare real RustDesk/RustDeskHop screenshots and replace the handmade title bar (text-symbol caption buttons and padded strip) with native Windows chrome, a matching light caption and compact window sizes. Screenshot-check the dashboard, maximize/restore and both editors at 100%; check native minimize/restore and the right-click system menu. Keep the window theme shared across every form.
- [x] Rework visual hierarchy after the compact-sizing pass: one primary Connect action, one shared list/action frame, quiet maintenance controls, neutral network badges, consistent spacing tokens and matching framed dialogs.
- [x] Adopt the approved blue-on-white icon with one editable master (`Assets/RustDeskHop.svg`), the original embedded bunny, precisely opposed diagonal ring openings, automatically generated build assets and automatic local shortcut refresh during deployment.
- [x] Refine the oversized UI while preserving the agreed visual style: smaller default windows, typography, buttons and spacing; text-sized computer rows; bounded dashboard/editor content when maximized; wrapping selected-computer details.
- [x] Check the main window and network manager at their minimum, default and maximized sizes at 100% Windows scaling; check the compact Add computer dialog. Fix input borders not repainting after resizing. Add regression coverage for layout containment, repeated resizing and long labels.
- [ ] Verify the main window and dialogs on real 125%, 150% and 200% Windows displays, including moving between monitors with different scaling. Keep all text readable and controls reachable; do not treat 100% screenshots as high-DPI validation.

User feedback recorded on 2026-09-17 and 2026-09-20. Compact sizing was followed by a separate hierarchy/framing pass on 2026-09-20. Higher-DPI/mixed-monitor validation remains open.

## Next milestone: tray-first UX

- [x] Add a system-tray mode: close/minimize hides, click/Open restores, Exit quits only the companion, and a second launch restores the existing instance.
- [ ] Add a tray menu with saved clients grouped by network profile.
- [ ] Add a polished network-switch confirmation popup.
- [ ] Show the current RustDesk network and the target network clearly in the popup.
- [ ] Show whether Tailscale/private-server access is ready before connecting.

## Client list and navigation

- [x] Replace the basic table with a cleaner, rounded computer list and selected-computer details.
- [ ] Add search/filtering to the computer list.
- [ ] Show client name, RustDesk ID, required network, and online/reachable status.
- [ ] Add last-connected time and recently used clients.
- [ ] Add client editing, not only add/remove.
- [ ] Add keyboard navigation and a quick-connect shortcut.
- [ ] Add configurable global hotkeys, for example `Ctrl+Shift+R`.

## Network profiles

- [x] Identify Public and private profiles with quiet, explicitly labeled badges.
- [ ] Add a profile status/test button.
- [ ] Add an “Open Tailscale” action when a private profile is unavailable.
- [ ] Support importing and exporting profiles and client mappings.
- [ ] Keep server keys in local configuration and never commit machine-specific values.
- [ ] Remember the last selected profile without rewriting RustDesk’s global configuration.

## Session safety

- [ ] Detect visible RustDesk connection windows more accurately.
- [ ] Tell the user exactly which sessions will be closed before switching.
- [ ] Keep the RustDesk background service and unattended access untouched.
- [ ] Allow connecting without closing existing sessions when the user chooses.
- [ ] Add clear error messages for unreachable private networks, missing keys, and offline clients.

## Windows integration

- [x] Add a proper application icon.
- [ ] Add Start Menu and optional startup integration.
- [ ] Add a lightweight installer or packaged release.
- [ ] Consider code signing for release builds.
- [ ] Add a setting for minimizing to the tray.

## Quality and release

- [ ] Add unit tests for profile loading, target mapping, and connection-target construction.
- [ ] Add integration tests for private-server reachability checks.
- [x] Add GitHub Actions for build and test verification.
- [ ] Publish versioned release artifacts.
- [ ] Add screenshots and a short usage guide to the README.

## Future ideas

- [ ] Support multiple private RustDesk servers.
- [ ] Optionally remember a server profile per saved client automatically.
- [ ] Add connection history and lightweight diagnostics.
- [ ] Explore native RustDesk integration if a standalone launcher becomes limiting.

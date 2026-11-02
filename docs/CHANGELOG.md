# Changelog

## 0.1.1-beta.7 — 2026-11-02

Fix saving networks and computers so **Save network** writes changes immediately and refreshes the main computer list.

- Keep saved changes after closing management with either Close or the window's X. Closing no longer performs a second, implicit save.
- Preserve editable drafts when a save fails, including the ability to correct a new computer's ID before retrying.
- Save only the selected network; keep other networks' unfinished edits and incomplete new networks out of the saved file.
- Persist confirmed network deletions before removing them from management. Keep the existing computer-row objects, focus and scrolling when saving.

All 290 tests and 27 release-tool checks passed in the fix's GitHub build, including installer/upgrade/launch/uninstall and release packaging checks. Settings format/location and connection behavior are retained. Account or cross-device synchronization is not included. This remains an unsigned beta, including when selected as GitHub's Latest release. See [testing evidence](TESTING.md).

## 0.1.1-beta.6 — 2026-11-02

Fix Windows startup DPI scaling while preserving the existing interface and fixed 90% design density.

- Scale windows, sidebar widths and nested network fields together with their text and buttons. At 150% Windows scaling, the dashboard and network manager now receive their full initial DPI layout instead of retaining 100% dimensions.
- Preserve the 96-DPI design baseline until each form has constructed its controls, and let nested editors inherit the initial scaling pass.
- Add regression coverage for nested field columns at simulated 100%, 150% and 200% startup scaling. All 282 local tests pass.

Saved networks/computers, settings compatibility, connection routing and installer identity are unchanged. The maintainer approved the corrected fictional-data preview; laptop testing and the wider manual-validation matrix remain pending. This remains an unsigned beta, including when selected as GitHub's Latest release. See [testing evidence](TESTING.md).

## 0.1.1-beta.5 — 2026-11-02

Maintenance release following 0.1.1-beta.4. The application source, saved-settings schema/location and installer identity remain unchanged.

- Update `actions/checkout` from 4.4.0 to 7.0.1 ([#26](https://github.com/Deucarian/RustDeskHop/pull/26)).
- Update `actions/setup-dotnet` from 4.3.1 to 6.0.0 while retaining the SDK selected by `global.json` ([#27](https://github.com/Deucarian/RustDeskHop/pull/27)).
- Update `actions/upload-artifact` from 4.6.2 to 7.0.2 with archive behavior retained ([#44](https://github.com/Deucarian/RustDeskHop/pull/44)).
- Align the provenance action pin with official v3.0.0 ([#25](https://github.com/Deucarian/RustDeskHop/pull/25)).
- Update xUnit from 2.9.2 to 2.9.3 ([#29](https://github.com/Deucarian/RustDeskHop/pull/29)), Microsoft.NET.Test.Sdk from 17.13.0 to 18.11.0 ([#45](https://github.com/Deucarian/RustDeskHop/pull/45)), and the Visual Studio adapter from 2.8.2 to 4.0.1 ([#46](https://github.com/Deucarian/RustDeskHop/pull/46)).

The older Test SDK and adapter proposals (#28 and #30) are superseded. This remains an unsigned prerelease under the existing readiness gates. See [testing evidence](TESTING.md) and [signing policy](CODE_SIGNING.md).

## 0.1.1-beta.4 — 2026-10-06

Inline computer editing, fixed interface density, joined navigation tabs, preserved management drafts, and sidebar hover-flicker correction. See the [published release notes](https://github.com/Deucarian/RustDeskHop/releases/tag/v0.1.1-beta.4) and [earlier release history](https://github.com/Deucarian/RustDeskHop/releases).

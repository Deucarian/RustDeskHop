# Code signing policy

## Current status

**Not enrolled or approved; public downloads remain unsigned until a release explicitly states otherwise.** No RustDeskHop public signing identity or signing-service credentials have been configured. Local development certificates for unrelated projects are not used. GitHub build attestations are separate from Windows Authenticode signatures and do not remove SmartScreen warnings.

Signing does not change the GPL-3.0-only license, charge users, restrict forks, or require a certificate to build the app. All build/installer scripts and matching source ship publicly. Do not disable Windows protection or install a self-signed certificate as a workaround.

## Free open-source signing route

The prepared workflow integrates [SignPath](https://docs.signpath.io/trusted-build-systems/github). The [SignPath Foundation application](https://signpath.org/apply.html) is subject to project review and approval, not an entitlement. No application or agreement has been submitted on the maintainer's behalf.

Proposed project roles: [Deucarian](https://github.com/Deucarian) maintains/reviews the repository and approves releases. These are proposed signing roles, not a claim of enrollment. Before enabling signing, the maintainer must satisfy the provider's current [conditions](https://signpath.org/terms.html), including MFA, reviewed contributions, explicit signing approval, metadata restrictions and verifiable GitHub-hosted builds. Provider approval cannot be replaced by workflow settings.

After acceptance, add the provider-required attribution to this policy and the README: “Free code signing provided by SignPath.io, certificate by SignPath Foundation.” Do not present that credit as an existing service before acceptance.

## Maintainer activation checklist

1. Apply, review the agreement, configure project roles and obtain approval. Confirm the [privacy policy](../PRIVACY.md) and [branding clearance](BRANDING.md).
2. Configure GitHub trusted-build origin checks and a manually approved release signing policy restricted to this repository and reviewed version tags. Never grant signing access to fork/PR jobs.
3. Configure two ZIP artifact definitions: one containing only `RustDeskHop.exe`, the other containing only `RustDeskHop-v<version>-win-x64-setup.exe`. Enforce `ProductName=RustDeskHop` and expected version metadata. Only sign our application and packaged setup, not Microsoft/runtime executables independently.
4. Add repository secret `SIGNPATH_API_TOKEN` with submit-only privileges. Add variables `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_POLICY_SLUG`, `SIGNPATH_APP_CONFIGURATION`, `SIGNPATH_INSTALLER_CONFIGURATION` using actual assigned values.
5. Test a prerelease end to end, then set `SIGNPATH_ENABLED=true`. The workflow uploads unsigned artifacts, waits for provider approval, downloads signed results and requires valid timestamped signatures. A signing failure stops publication; there is no unsigned fallback when enabled.
6. Verify app and setup signatures on another Windows PC. The installer-generated uninstaller is not separately signed by this workflow. Confirm that packaging arrangement with the provider. Never advertise broader signing coverage than verified.

Stable official releases additionally require the recorded manual-validation and branding gates in `release-readiness.json`. These protect official distribution, not contributors' freedom to build or fork. Release assets include hashes, provenance attestations, matching source and runtime notices. Previously published versions are never silently replaced.

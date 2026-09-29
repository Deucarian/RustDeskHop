# Contributing and testing

Anyone can test RustDeskHop, [report a bug](https://github.com/Deucarian/RustDeskHop/issues/new?template=bug_report.yml), propose a feature, or submit a pull request. A GitHub account is needed to file an issue or PR; no membership or tester registration is required.

Start with a prerelease and non-critical machines. Keep a working fallback connection. Never change a remote machine's incoming/default network while it is your only access route.

1. Read the [README](README.md), [privacy policy](PRIVACY.md), and [testing checklist](docs/TESTING.md).
2. Use a supported Windows 11 x64 environment and the .NET SDK selected by `global.json`.
3. Branch from `develop`, make a focused change, and run `dotnet test tests/RustDeskHop.Tests/RustDeskHop.Tests.csproj -c Release`.
4. Open a PR against `develop`; describe behavior, tests, limitations and screenshots for UI changes. Maintainers promote reviewed changes to `main`.

Do not commit machine settings, real device IDs, tokens, passwords, binary build outputs or generated icon variants. The single branding master is `Assets/RustDeskHop.svg`. Security issues go through [private reporting](SECURITY.md).

Contributions to original project code/docs are under GPL-3.0-only. Keep existing notices and identify third-party material with its source and license. Do not claim rights to third-party trademarks. See [branding status](docs/BRANDING.md).

Contributing does not assign copyright to the maintainer or to RustDesk. Future upstream collaboration is welcome, but any copyright assignment would require a separate agreement with the relevant rights holders. The project's promise of free official downloads is not a restriction on GPL-permitted commercial use or redistribution.

Be respectful, assume good intent, and keep feedback specific and actionable. This is a volunteer project, not a support contract.

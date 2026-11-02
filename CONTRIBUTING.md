# Contributing and testing

Anyone can test RustDeskHop, [report a bug](https://github.com/Deucarian/RustDeskHop/issues/new?template=bug_report.yml), propose a feature, or submit a pull request. A GitHub account is needed to file an issue or PR; no membership or tester registration is required.

Start with a prerelease and non-critical machines. Keep a working fallback connection. Never change a remote machine's incoming/default network while it is your only access route.

1. Read the [README](README.md), [privacy policy](PRIVACY.md), and [testing checklist](docs/TESTING.md).
2. Use a supported Windows 11 x64 environment and the .NET SDK selected by `global.json`.
3. Open `RustDeskHop.slnx`, branch from `develop`, make a focused change, and run `dotnet test RustDeskHop.slnx -c Release`.
4. Open a PR against `develop`; describe behavior, tests, limitations and screenshots for UI changes. Maintainers promote reviewed changes to `main`.

Do not commit machine settings, real device IDs, tokens, passwords, binary build outputs or generated icon variants. The single branding master is `Assets/RustDeskHop.svg`. Security issues go through [private reporting](SECURITY.md).

See the [code structure and composition guide](README.md#code-structure-and-composition) before extending the app. Keep connection decisions in `Connections/`, Windows side effects in `Integrations/`, and presentation in `UI/`. Wire production dependencies in `ApplicationComposition`; do not construct installed clients inside forms or workflow classes. Extend the real-service/fake-adapter tests when changing connection behavior. Preserve the settings path/schema, installer identity and existing consent/backup/rollback guards.

## C# conventions

The repository's [.editorconfig](.editorconfig) is the shared source of formatting and naming rules. [RustDeskHop.slnx.DotSettings](RustDeskHop.slnx.DotSettings) supplies the companion Rider/ReSharper member layout. Both are self-contained: contributors do not need the maintainer's personal settings or another repository.

- Four spaces, block-scoped namespaces, explicit local types and explicitly typed construction. Anonymous types may use `var`.
- Private fields: `_camelCase`; other fields: `camelCase`; constants and enum members: `UPPER_SNAKE_CASE`; types, methods and properties: `PascalCase`; interfaces: `IPascalCase`; parameters and other locals: `camelCase`.
- A 120-character formatting target. Short calls stay compact; long calls align arguments under the first argument, with separate closing-parenthesis lines, including nested constructors. Wrapped declaration parameters align too, but their closing parenthesis stays on the final parameter line. Long chains/conditions use leading dots/operators. Indivisible literals and identifiers may exceed the target.
- Populated class sections use indented regions: constants/fields, constructors/destructors, delegates/events, enums/interfaces, properties/indexers, setup/teardown, tests, methods, nested types. Preserve initializer evaluation order, enum values and interop layout. xUnit test methods are recognized by the layout.
- Braces around loop bodies and multiline conditional bodies; simple guards may stay unbraced on a separate line. Preserve framework overrides, JSON property names, binding names and other compatibility contracts.

Run **Reformat Code** explicitly in Rider/ReSharper when needed. Hard wrapping while typing stays disabled. No formatter package, on-save hook or build-time formatting step is required. Visual Studio can use the standard EditorConfig settings; its native formatter is not expected to reproduce JetBrains-specific wrapping/member layout exactly. Avoid broad cleanup that changes behavior or initialization order. Generated files and third-party code are outside convention-only changes.

## Licensing and community

Contributions to original project code/docs are under GPL-3.0-only. Keep existing notices and identify third-party material with its source and license. Do not claim rights to third-party trademarks. See [branding status](docs/BRANDING.md).

Contributing does not assign copyright to the maintainer or to RustDesk. Future upstream collaboration is welcome, but any copyright assignment would require a separate agreement with the relevant rights holders. The project's promise of free official downloads is not a restriction on GPL-permitted commercial use or redistribution.

Be respectful, assume good intent, and keep feedback specific and actionable. This is a volunteer project, not a support contract.

# Third-party components and notices

RustDeskHop's original code is GPL-3.0-only; see `LICENSE`. This does not relicense third-party components or grant rights to trademarks.

## Distributed Windows runtime

Self-contained downloads include Microsoft's .NET runtime and Windows Desktop runtime under their own licenses and third-party notices. `tools/Collect-ReleaseNotices.ps1` copies the **actual resolved runtime packages'** license and notice files into `licenses/`, records their package versions/hashes, and fails if the expected notices cannot be found. They must accompany every redistribution, including a standalone EXE (download the matching notices ZIP). See [runtime sources](https://github.com/dotnet/runtime), [Windows Forms sources](https://github.com/dotnet/winforms), and the source links in those packages/notices.

## Installer

Installers use the unmodified [Inno Setup](https://jrsoftware.org/) engine, copyright Jordan Russell and Martijn Laan, under the [Inno Setup License](https://jrsoftware.org/files/is/license.txt). Its existing embedded notices are preserved; the compiler's own license is copied into the release notices. The GPL covers our installer script, not a relicensing of that engine.

## Build/test-only dependencies

The SVG renderer uses SVG.NET and its dependencies only while building branding. Tests use xUnit, Microsoft.NET.Test.Sdk and their dependencies. Their versions, declared licenses and source repositories can be inspected in the NuGet metadata for the project package references and resolved assets. These renderer/test assemblies must not be copied into the app's published directory. Dependency notices for redistributed .NET components are not replaced by this inventory.

## RustDesk and branding

RustDesk is separately installed and is not included in the downloads. It retains [its own license](https://github.com/rustdesk/rustdesk/blob/master/LICENCE). RustDesk's name, logo and trademarks are not licensed by RustDeskHop. The companion artwork's provenance and unresolved clearance are tracked in [BRANDING.md](docs/BRANDING.md).

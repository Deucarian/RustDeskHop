[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')][string]$Version,
    [Parameter(Mandatory)][string]$PublishDirectory,
    [Parameter(Mandatory)][string]$InstallerDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$SigningEnabled
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Test-Publish.ps1') -PublishDirectory $PublishDirectory
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a fresh release directory; never overwrite release assets.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$prefix = "RustDeskHop-v$Version"
Copy-Item -LiteralPath (Join-Path $PublishDirectory 'RustDeskHop.exe') -Destination (Join-Path $OutputDirectory "$prefix-win-x64.exe")
Copy-Item -LiteralPath (Join-Path $InstallerDirectory "$prefix-win-x64-setup.exe") -Destination $OutputDirectory
Compress-Archive -Path (Join-Path $PublishDirectory '*') -DestinationPath (Join-Path $OutputDirectory "$prefix-win-x64.zip")
Compress-Archive -Path (Join-Path $PublishDirectory 'licenses'), (Join-Path $PublishDirectory 'LICENSE'), (Join-Path $PublishDirectory 'THIRD-PARTY-NOTICES.md') -DestinationPath (Join-Path $OutputDirectory "$prefix-notices.zip")
git archive --format=zip "--prefix=$prefix/" "--output=$(Join-Path $OutputDirectory "$prefix-source.zip")" HEAD
if ($LASTEXITCODE -ne 0) { throw 'Matching source archive failed' }
Copy-Item -LiteralPath (Join-Path $PublishDirectory 'LICENSE') -Destination $OutputDirectory
$signatureStatus = if ($SigningEnabled -eq 'true') { 'Application and setup EXE: Authenticode signed and timestamped. The installer-generated uninstaller is not separately signed.' } else { 'UNSIGNED TEST BUILD: no trusted Authenticode publisher signature. Windows may warn. Do not disable Windows security protections.' }
$limitations = if ($Version.Contains('-')) { 'Opt-in beta: real mixed-DPI/alternate-account and cross-version end-to-end checks are not complete; branding clearance is still pending. Not a broad production-readiness claim.' } else { 'Public release gates passed; see the versioned testing evidence and branding record.' }
$notes = @"
$signatureStatus

$limitations

Free open-source software under GPL-3.0-only. Matching source and build instructions are in $prefix-source.zip. Prefer the installer or portable ZIP: they include licenses, notices and usage documentation. Standalone EXE users must also retain $prefix-notices.zip when redistributing. RustDesk is installed separately.

Code signing policy: https://github.com/Deucarian/RustDeskHop/blob/v$Version/docs/CODE_SIGNING.md
Testing and known limitations: https://github.com/Deucarian/RustDeskHop/blob/v$Version/docs/TESTING.md
Bug reports and feature ideas: https://github.com/Deucarian/RustDeskHop/issues/new/choose
Security reports: https://github.com/Deucarian/RustDeskHop/security/advisories/new

SHA256SUMS.txt covers every attached binary/archive/license. GitHub build provenance is available using: gh attestation verify <downloaded-file> --repo Deucarian/RustDeskHop
"@
$notes | Set-Content -LiteralPath 'release-notes.md' -Encoding utf8
$notes | Set-Content -LiteralPath (Join-Path $OutputDirectory 'RELEASE-INFO.txt') -Encoding utf8
Get-ChildItem -LiteralPath $OutputDirectory -File | Sort-Object Name | ForEach-Object {
    "$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)  $($_.Name)"
} | Set-Content -LiteralPath (Join-Path $OutputDirectory 'SHA256SUMS.txt') -Encoding ascii

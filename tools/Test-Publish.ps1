[CmdletBinding()]
param([Parameter(Mandatory)][string]$PublishDirectory)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PublishDirectory).Path
foreach ($name in @('RustDeskHop.exe', 'LICENSE', 'README.md', 'PRIVACY.md', 'SECURITY.md', 'CONTRIBUTING.md', 'THIRD-PARTY-NOTICES.md', 'Assets\RustDeskHop.svg', 'Assets\RustDeskHop.ico', 'Assets\RustDeskHop.png', 'docs\CODE_SIGNING.md', 'docs\BRANDING.md', 'docs\TESTING.md', 'docs\images\dashboard.png', 'licenses\runtime-and-installer-inventory.json')) {
    if (!(Test-Path -LiteralPath (Join-Path $root $name) -PathType Leaf)) { throw "Missing distribution file: $name" }
}
$files = @(Get-ChildItem -LiteralPath $root -File -Recurse)
$unwanted = @($files | Where-Object { $_.Name -match '(?i)(settings.*\.json|\.toml$|\.pfx$|\.pem$|\.key$|testhost|xunit|^Svg\.dll$|^ExCSS\.dll$|\.pdb$)' })
if ($unwanted.Count) { throw "Private/build-only files must not ship: $($unwanted.Name -join ', ')" }
$metadata = (Get-Item (Join-Path $root 'RustDeskHop.exe')).VersionInfo
if ($metadata.ProductName -ne 'RustDeskHop') { throw 'Unexpected executable product name' }
Write-Output "Publish layout verified: $($files.Count) files; product $($metadata.ProductName) $($metadata.ProductVersion)."

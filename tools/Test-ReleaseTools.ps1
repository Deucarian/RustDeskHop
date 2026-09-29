[CmdletBinding()]
param([Parameter(Mandatory)][string]$PublishDirectory)
$ErrorActionPreference = 'Stop'
$passed = 0
function Assert-Rejected([scriptblock]$Action, [string]$Expected) {
    $message = $null
    try { & $Action } catch { $message = $_.Exception.Message }
    if (!$message -or !$message.Contains($Expected)) { throw "Expected rejection containing '$Expected', got '$message'" }
    $script:passed++
}
& (Join-Path $PSScriptRoot 'Test-ReleaseGate.ps1') -Version '0.1.1-beta.1'
$passed++
Assert-Rejected { & (Join-Path $PSScriptRoot 'Test-ReleaseGate.ps1') -Version '0.1.1' } 'Stable public release blocked'
Assert-Rejected { & (Join-Path $PSScriptRoot 'Test-ReleaseGate.ps1') -Version 'not-a-version' } 'Invalid release version'
Assert-Rejected { & (Join-Path $PSScriptRoot 'Test-Signature.ps1') -Path (Join-Path $PublishDirectory 'RustDeskHop.exe') } 'Expected a valid timestamped'
& (Join-Path $PSScriptRoot 'Test-Publish.ps1') -PublishDirectory $PublishDirectory
$passed++
$root = Join-Path ([IO.Path]::GetTempPath()) ('RustDeskHop-release-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$copy = Join-Path $root 'publish'
Copy-Item -LiteralPath $PublishDirectory -Destination $copy -Recurse
# Every write/delete below belongs to this test's fresh isolated directory.
foreach ($name in @('settings.local.json', 'RustDesk2.toml', 'signing.pfx', 'xunit.dll', 'Svg.dll')) {
    $file = Join-Path $copy $name
    'test fixture, not private data' | Set-Content -LiteralPath $file
    Assert-Rejected { & (Join-Path $PSScriptRoot 'Test-Publish.ps1') -PublishDirectory $copy } 'Private/build-only files must not ship'
    Remove-Item -LiteralPath $file
}
$license = Join-Path $copy 'LICENSE'
Remove-Item -LiteralPath $license
Assert-Rejected { & (Join-Path $PSScriptRoot 'Test-Publish.ps1') -PublishDirectory $copy } 'Missing distribution file: LICENSE'
Write-Output "PASS: $passed release-tool checks. Isolated fixture retained at $root"

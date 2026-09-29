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
Assert-Rejected { & (Join-Path $PSScriptRoot 'Test-ReleaseGate.ps1') -Version 'not-a-version' } 'Invalid release version'
Assert-Rejected { & (Join-Path $PSScriptRoot 'Test-Signature.ps1') -Path (Join-Path $PublishDirectory 'RustDeskHop.exe') } 'Expected a valid timestamped'
& (Join-Path $PSScriptRoot 'Test-Publish.ps1') -PublishDirectory $PublishDirectory
$passed++
$root = Join-Path ([IO.Path]::GetTempPath()) ('RustDeskHop-release-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$readinessPath = Join-Path $root 'release-readiness.json'
# Signing must never replace evidence or become an enrollment requirement.
# Only these fixtures change; the repository's real evidence remains untouched.
foreach ($signing in @('', 'false', 'true')) {
    foreach ($branding in @($false, $true)) {
        foreach ($manual in @($false, $true)) {
            @{ brandingCleared = $branding; manualValidationComplete = $manual } |
                ConvertTo-Json | Set-Content -LiteralPath $readinessPath
            $check = { & (Join-Path $PSScriptRoot 'Test-ReleaseGate.ps1') -Version '0.1.1' -SigningEnabled $signing -ReadinessPath $readinessPath }
            if ($branding -and $manual) {
                $result = & $check
                $expected = if ($signing -eq 'true') { 'Signing enabled:' } else { 'Unsigned release allowed:' }
                if (!(($result -join "`n").Contains($expected))) { throw "Missing signature disclosure: $expected" }
                $passed++
            }
            else { Assert-Rejected $check 'Stable public release blocked' }
        }
    }
}
foreach ($fixture in @('{}', '{"brandingCleared":"true","manualValidationComplete":true}', '{"brandingCleared":true,"manualValidationComplete":"true"}')) {
    $fixture | Set-Content -LiteralPath $readinessPath
    Assert-Rejected { & (Join-Path $PSScriptRoot 'Test-ReleaseGate.ps1') -Version '0.1.1' -SigningEnabled 'true' -ReadinessPath $readinessPath } 'Stable public release blocked'
}
'not JSON' | Set-Content -LiteralPath $readinessPath
Assert-Rejected { & (Join-Path $PSScriptRoot 'Test-ReleaseGate.ps1') -Version '0.1.1' -ReadinessPath $readinessPath } 'Cannot read release readiness evidence'
Remove-Item -LiteralPath $readinessPath
Assert-Rejected { & (Join-Path $PSScriptRoot 'Test-ReleaseGate.ps1') -Version '0.1.1' -ReadinessPath $readinessPath } 'Cannot read release readiness evidence'
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

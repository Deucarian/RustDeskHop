[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [ValidateSet('', 'false', 'true')][string]$SigningEnabled,
    # CI uses the committed evidence by default; tests supply an isolated fixture.
    [string]$ReadinessPath = (Join-Path $PSScriptRoot '..\docs\release-readiness.json')
)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') { throw 'Invalid release version' }
if ($Version.Contains('-')) {
    Write-Output 'Prerelease: clearly disclose signature status and remaining validation/branding limitations.'
}
else {
    try { $readiness = Get-Content -LiteralPath $ReadinessPath -Raw | ConvertFrom-Json }
    catch { throw "Cannot read release readiness evidence: $($_.Exception.Message)" }
    if ($readiness.brandingCleared -isnot [bool] -or !$readiness.brandingCleared -or
        $readiness.manualValidationComplete -isnot [bool] -or !$readiness.manualValidationComplete) {
        throw 'Stable public release blocked: record branding clearance and complete manual validation. Signing is optional. Use a prerelease for opt-in testers.'
    }
    Write-Output 'Stable release: branding and manual-validation gates passed.'
}
if ($SigningEnabled -eq 'true') {
    Write-Output 'Signing enabled: publication requires verified timestamped app/setup signatures; no unsigned fallback.'
}
else {
    Write-Output 'Unsigned release allowed: disclose that Windows may warn or block this download.'
}

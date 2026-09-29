[CmdletBinding()]
param([Parameter(Mandatory)][string]$Version, [string]$SigningEnabled)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') { throw 'Invalid release version' }
if ($Version.Contains('-')) {
    Write-Output 'Prerelease: clearly disclose unsigned status and remaining validation/branding limitations.'
}
else {
    $readiness = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\docs\release-readiness.json') -Raw | ConvertFrom-Json
    if (!$readiness.brandingCleared -or !$readiness.manualValidationComplete -or $SigningEnabled -ne 'true') {
        throw 'Stable public release blocked: record branding clearance, complete manual validation, and enable approved signing. Use a prerelease for opt-in testers.'
    }
}

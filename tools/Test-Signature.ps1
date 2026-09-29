[CmdletBinding()]
param([Parameter(Mandatory)][string]$Path)
$ErrorActionPreference = 'Stop'
$signature = Get-AuthenticodeSignature -LiteralPath $Path
if ($signature.Status -ne 'Valid' -or !$signature.TimeStamperCertificate) {
    throw "Expected a valid timestamped Authenticode signature: $Path ($($signature.Status))"
}
Write-Output "Verified timestamped signature: $Path; signer: $($signature.SignerCertificate.Subject)"

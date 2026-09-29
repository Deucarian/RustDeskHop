[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')][string]$Version,
    [string]$Compiler = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $Compiler)) { throw 'Install Inno Setup 6.4+ or pass -Compiler. Do not install it automatically.' }
& (Join-Path $PSScriptRoot 'Test-Publish.ps1') -PublishDirectory $PublishDirectory
$publishPath = (Resolve-Path -LiteralPath $PublishDirectory).Path
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
& $Compiler "/DPublishDir=$publishPath" "/DOutputDir=$outputPath" "/DAppVersion=$Version" "/DNumericVersion=$($Version.Split('-')[0]).0" (Join-Path $PSScriptRoot '..\packaging\RustDeskHop.iss')
if ($LASTEXITCODE -ne 0) { throw "Installer compiler failed: $LASTEXITCODE" }

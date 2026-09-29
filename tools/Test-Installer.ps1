# Deliberately limited to disposable GitHub-hosted test environments.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Installer)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true' -or !$env:RUNNER_TEMP) { throw 'Run installer smoke tests on a disposable GitHub Actions runner, not your workstation.' }
if (Get-Process -Name RustDeskHop -ErrorAction SilentlyContinue) { throw 'A RustDeskHop instance is already running; refusing to interfere.' }
$testRoot = Join-Path $env:RUNNER_TEMP ("rustdeskhop-install-test-" + [guid]::NewGuid().ToString('N'))
$installPath = Join-Path $testRoot 'app'
New-Item -ItemType Directory -Path $testRoot | Out-Null
$startLink = Join-Path ([Environment]::GetFolderPath('Programs')) 'RustDeskHop.lnk'
$startupLink = Join-Path ([Environment]::GetFolderPath('Startup')) 'RustDeskHop.lnk'
if ((Test-Path -LiteralPath $startLink) -or (Test-Path -LiteralPath $startupLink)) { throw 'Existing companion shortcuts must not be changed by smoke tests.' }
$settingsPath = Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'SimultriaRustDeskCompanion\settings.json'
if (Test-Path -LiteralPath $settingsPath) { throw 'Existing settings must not be changed by smoke tests.' }
function Run-Setup([string]$Path, [string]$Arguments) {
    $process = Start-Process -FilePath $Path -ArgumentList $Arguments -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(60000)) { throw 'Installer/uninstaller did not finish within 60 seconds.' }
    if ($process.ExitCode -ne 0) { throw "Installer/uninstaller failed: $($process.ExitCode)" }
}
$installerPath = (Resolve-Path -LiteralPath $Installer).Path
Run-Setup $installerPath "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=`"$installPath`" /TASKS=`"`" /LOG=`"$testRoot\install.log`""
if (!(Test-Path -LiteralPath $startLink)) { throw 'Start Menu shortcut missing' }
if (Test-Path -LiteralPath $startupLink) { throw 'Startup must default to off' }
$shell = New-Object -ComObject WScript.Shell
if ($shell.CreateShortcut($startLink).TargetPath -ne (Join-Path $installPath 'RustDeskHop.exe')) { throw 'Start Menu target is wrong' }
$app = Start-Process -FilePath (Join-Path $installPath 'RustDeskHop.exe') -ArgumentList '--verify-install' -PassThru -WindowStyle Hidden
try {
    if (!$app.WaitForExit(20000)) { throw 'Published application self-check timed out' }
    if ($app.ExitCode -ne 0) { throw "Published application self-check failed: $($app.ExitCode)" }
    if (Test-Path -LiteralPath $settingsPath) { throw 'Opening the app must not persist default settings' }
} finally {
    # Only the exact process started by this test; never RustDesk or any service.
    if (!$app.HasExited) { $app.Kill(); $app.WaitForExit() }
}
$sentinel = Join-Path $installPath 'user-owned-file.txt'
'preserve me' | Set-Content -LiteralPath $sentinel
New-Item -ItemType Directory -Path (Split-Path -Parent $settingsPath) -Force | Out-Null
'{"Profiles":[],"Targets":[]}' | Set-Content -LiteralPath $settingsPath
$originalHash = (Get-FileHash -LiteralPath $settingsPath).Hash
# Upgrade in place and opt into startup; neither operation should touch settings.
Run-Setup $installerPath "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=`"$installPath`" /TASKS=`"startup`" /LOG=`"$testRoot\upgrade.log`""
if (!(Test-Path -LiteralPath $startupLink)) { throw 'Opt-in startup shortcut missing' }
Run-Setup (Join-Path $installPath 'unins000.exe') "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG=`"$testRoot\uninstall.log`""
foreach ($removed in @((Join-Path $installPath 'RustDeskHop.exe'), $startLink, $startupLink)) {
    if (Test-Path -LiteralPath $removed) { throw "Uninstall left an owned file: $removed" }
}
if (!(Test-Path -LiteralPath $sentinel)) { throw 'Uninstall removed user-owned data' }
if ((Get-FileHash -LiteralPath $settingsPath).Hash -ne $originalHash) { throw 'Install/upgrade/uninstall changed user settings' }
Write-Output 'PASS: install, Start Menu, startup opt-in, published EXE/form self-check, upgrade, uninstall and retained user data.'

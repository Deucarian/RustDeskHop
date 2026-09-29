[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$AssetsFile,
    [Parameter(Mandatory)][string]$PublishDirectory,
    [string]$InnoCompilerDirectory
)
$ErrorActionPreference = 'Stop'
$assets = Get-Content -LiteralPath $AssetsFile -Raw | ConvertFrom-Json -AsHashtable
$output = Join-Path (Resolve-Path -LiteralPath $PublishDirectory).Path 'licenses'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$inventory = @()
foreach ($prefix in @('Microsoft.NETCore.App.Runtime.win-x64', 'Microsoft.WindowsDesktop.App.Runtime.win-x64')) {
    $dependency = @($assets.project.frameworks.Values.downloadDependencies | Where-Object name -eq $prefix)
    if ($dependency.Count -ne 1) { throw "Expected one resolved runtime package for $prefix" }
    $range = $dependency[0].version
    if ($range -notmatch '^\[([^,\]]+)(?:,\s*\1)?\]$') { throw "Unresolved runtime version: $range" }
    $version = $Matches[1]
    $packagePath = Join-Path $prefix.ToLowerInvariant() $version
    $roots = @($assets.packageFolders.Keys | ForEach-Object { Join-Path $_ $packagePath } | Where-Object { Test-Path -LiteralPath $_ })
    if ($roots.Count -eq 0) { throw "Runtime package missing: $packagePath" }
    $root = $roots[0]
    $documents = @(Get-ChildItem -LiteralPath $root -File | Where-Object { $_.Name -match '^(LICENSE|THIRD-PARTY-NOTICES)(\.(txt|md))?$' })
    # The desktop pack supplies LICENSE without an extension and currently no
    # separate notices file. The core runtime pack supplies both documents.
    if (@($documents | Where-Object Name -match '^LICENSE').Count -eq 0 -or
        ($prefix -eq 'Microsoft.NETCore.App.Runtime.win-x64' -and @($documents | Where-Object Name -match '^THIRD-PARTY').Count -eq 0)) {
        throw "Missing license or third-party notices in $packagePath"
    }
    foreach ($document in $documents) {
        Copy-Item -LiteralPath $document.FullName -Destination (Join-Path $output "$prefix-$version-$($document.Name)")
    }
    $inventory += [ordered]@{
        package = $prefix; version = $version
        source = "https://www.nuget.org/packages/$prefix/$version"
        packageSha512 = (Get-Content -LiteralPath (Join-Path $root "$($prefix.ToLowerInvariant()).$version.nupkg.sha512") -Raw).Trim()
    }
}
if ($InnoCompilerDirectory) {
    $license = Join-Path $InnoCompilerDirectory 'license.txt'
    if (!(Test-Path -LiteralPath $license)) { throw 'Inno Setup license is missing' }
    Copy-Item -LiteralPath $license -Destination (Join-Path $output 'Inno-Setup-LICENSE.txt')
    $inventory += [ordered]@{
        package = 'Inno Setup'; version = (Get-Item (Join-Path $InnoCompilerDirectory 'ISCC.exe')).VersionInfo.ProductVersion
        source = 'https://github.com/jrsoftware/issrc'
    }
}
$inventory | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'runtime-and-installer-inventory.json') -Encoding utf8
Write-Output "Copied resolved runtime notices to $output"

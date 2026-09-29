[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$InstallDirectory, [string]$BackupDirectory)

$ErrorActionPreference = 'Stop'
$installPath = (Resolve-Path -LiteralPath $InstallDirectory).Path
$executable = Join-Path $installPath 'RustDeskHop.exe'
$icon = Join-Path $installPath 'Assets\RustDeskHop.ico'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw "Missing $executable" }
if (-not (Test-Path -LiteralPath $icon -PathType Leaf)) { throw "Missing generated icon: $icon" }

if (-not ('RustDeskHopShellIcons' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Runtime.InteropServices;
public static class RustDeskHopShellIcons
{
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    private struct FileInfo {
        public IntPtr Icon;
        public int IconIndex;
        public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)] public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=80)] public string TypeName;
    }
    [DllImport("shell32.dll", CharSet=CharSet.Unicode, EntryPoint="SHGetFileInfoW")]
    private static extern IntPtr GetFileInfo(string path, uint attributes, ref FileInfo info, uint size, uint flags);
    [DllImport("shell32.dll", CharSet=CharSet.Unicode, EntryPoint="SHUpdateImageW")]
    private static extern void UpdateImage(string iconFile, int iconIndex, uint flags, int imageIndex);
    [DllImport("shell32.dll", CharSet=CharSet.Unicode, EntryPoint="ExtractIconExW")]
    private static extern uint CountIconGroups(string file, int index, IntPtr large, IntPtr small, uint count);
    [DllImport("shell32.dll", CharSet=CharSet.Unicode)]
    private static extern void SHChangeNotify(uint eventId, uint flags, string item, IntPtr unused);
    [DllImport("ole32.dll")] private static extern int CoInitialize(IntPtr reserved);
    [DllImport("ole32.dll")] private static extern void CoUninitialize();

    public static void NotifyItem(string path) {
        // SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSH: this file only.
        SHChangeNotify(0x2000, 0x1005, path, IntPtr.Zero);
    }

    public static bool RefreshExecutable(string path) {
        int initialized = CoInitialize(IntPtr.Zero);
        try {
            var location = new FileInfo();
            var image = new FileInfo();
            uint size = (uint)Marshal.SizeOf(typeof(FileInfo));
            // SHGFI_ICONLOCATION delegates to the file's IExtractIcon handler.
            if (GetFileInfo(path, 0, ref location, size, 0x1000) == IntPtr.Zero)
                return false;
            // Some Windows EXE handlers return no filename. Only fall back for
            // our verified single-icon executable, never a shared file-type icon.
            if (String.IsNullOrWhiteSpace(location.DisplayName)) {
                if (CountIconGroups(path, -1, IntPtr.Zero, IntPtr.Zero, 0) != 1) return false;
                location.DisplayName = path;
                location.IconIndex = 0;
            }
            if (!File.Exists(location.DisplayName) ||
                !StringComparer.OrdinalIgnoreCase.Equals(Path.GetFullPath(path), Path.GetFullPath(location.DisplayName)) ||
                GetFileInfo(path, 0, ref image, size, 0x4000) == IntPtr.Zero)
                return false;
            // The path/index stay the same when the executable is replaced.
            // File-backed EXE icons use neither GIL_NOTFILENAME nor GIL_SIMULATEDOC.
            UpdateImage(location.DisplayName, location.IconIndex, 0, image.IconIndex);
            NotifyItem(path);
            return true;
        } finally { if (initialized >= 0) CoUninitialize(); }
    }
}
'@
}

# Windows caches icons by path. This is a derived cache entry, not another master.
$hashAlgorithm = [Security.Cryptography.SHA256]::Create()
try {
    # Direct read works under -WhatIf in both Windows PowerShell 5.1 and PS7.
    $hash = ([BitConverter]::ToString($hashAlgorithm.ComputeHash([IO.File]::ReadAllBytes($icon))) -replace '-', '').Substring(0, 16)
} finally { $hashAlgorithm.Dispose() }
$cachedIcon = Join-Path $installPath "Assets\RustDeskHop-$hash.ico"
if ($PSCmdlet.ShouldProcess($cachedIcon, 'Refresh generated icon cache entry')) {
    Copy-Item -LiteralPath $icon -Destination $cachedIcon -Force
}

$shortcutPaths = @(
    (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\RustDeskHop.lnk'),
    (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Startup\RustDeskHop.lnk'),
    (Join-Path $env:APPDATA 'Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\RustDeskHop.lnk')
)
$shell = New-Object -ComObject WScript.Shell
for ($index = 0; $index -lt $shortcutPaths.Count; $index++) {
    $path = $shortcutPaths[$index]
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $shortcut = $shell.CreateShortcut($path)
    if (-not [StringComparer]::OrdinalIgnoreCase.Equals($shortcut.TargetPath, $executable)) { continue }
    if ($PSCmdlet.ShouldProcess($path, 'Use icon generated from the application master')) {
        if ($BackupDirectory) { Copy-Item -LiteralPath $path -Destination (Join-Path $BackupDirectory "shortcut-$index.lnk") }
        $shortcut.IconLocation = "$cachedIcon,0"
        $shortcut.Save()
        [RustDeskHopShellIcons]::NotifyItem($path)
        Write-Output "Updated RustDeskHop icon: $path"
    }
}

# New pins use the executable rather than our explicit shortcut icon path.
# Refresh its own shell-cache entry too; never reset all icons or restart Explorer.
if ($PSCmdlet.ShouldProcess($executable, 'Refresh this executable''s Windows shell icon')) {
    if ([RustDeskHopShellIcons]::RefreshExecutable($executable)) {
        Write-Output "Refreshed RustDeskHop executable icon: $executable"
    } else {
        Write-Warning 'Could not identify the executable-specific shell icon; no shared icon cache was changed.'
    }
}

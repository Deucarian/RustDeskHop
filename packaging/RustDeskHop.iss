; RustDeskHop installer source, GPL-3.0-only. Build with Inno Setup 6.4 or later.
#ifndef PublishDir
  #error PublishDir is required
#endif
#ifndef AppVersion
  #error AppVersion is required
#endif
#ifndef OutputDir
  #error OutputDir is required
#endif

[Setup]
AppId={{D5BFC459-E526-4C35-9A8C-38B9CD9EB528}
AppName=RustDeskHop
AppVersion={#AppVersion}
AppPublisher=RustDeskHop contributors
AppPublisherURL=https://github.com/Deucarian/RustDeskHop
AppSupportURL=https://github.com/Deucarian/RustDeskHop/issues
AppUpdatesURL=https://github.com/Deucarian/RustDeskHop/releases
VersionInfoProductName=RustDeskHop
VersionInfoVersion={#NumericVersion}
DefaultDirName={localappdata}\Programs\RustDeskHop
PrivilegesRequired=lowest
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0.22000
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=RustDeskHop-v{#AppVersion}-win-x64-setup
SetupIconFile={#PublishDir}\Assets\RustDeskHop.ico
UninstallDisplayIcon={app}\RustDeskHop.exe
InfoBeforeFile=installer-info.txt
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no
Uninstallable=yes

[Tasks]
Name: startup; Description: "Start RustDeskHop when I sign in to Windows"; Flags: unchecked
Name: desktopicon; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "{#PublishDir}\RustDeskHop.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\PRIVACY.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\SECURITY.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\CONTRIBUTING.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "{#PublishDir}\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishDir}\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\RustDeskHop"; Filename: "{app}\RustDeskHop.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\RustDeskHop"; Filename: "{app}\RustDeskHop.exe"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{userstartup}\RustDeskHop"; Filename: "{app}\RustDeskHop.exe"; WorkingDir: "{app}"; Tasks: startup

; No Run/UninstallRun hooks, service changes, broad UninstallDelete entries,
; or configuration deletion. Inno removes only its installed files/shortcuts.

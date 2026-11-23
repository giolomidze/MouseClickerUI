; AppVersion is normally passed in by build-installer.ps1 via ISCC /DAppVersion=x.y.z
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
; AppId uniquely identifies this app for upgrades/uninstall. NEVER change it.
AppId={{455A129D-F9A1-4633-B221-F10BA749A8BF}
AppName=Mouse Clicker UI
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
AppPublisher=App Dev Space
DefaultDirName={autopf}\MouseClickerUI
DefaultGroupName=Mouse Clicker UI
AllowNoIcons=yes
OutputDir=dist
OutputBaseFilename=MouseClickerUI-Setup
SetupIconFile=app.ico
Compression=lzma2
SolidCompression=yes
; Install per-user without admin by default; the user can choose an all-users install in a dialog.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\MouseClickerUI.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Mouse Clicker UI"; Filename: "{app}\MouseClickerUI.exe"
Name: "{group}\{cm:UninstallProgram,Mouse Clicker UI}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Mouse Clicker UI"; Filename: "{app}\MouseClickerUI.exe"; Tasks: desktopicon; IconFilename: "{app}\MouseClickerUI.exe"

[Run]
Filename: "{app}\MouseClickerUI.exe"; Description: "{cm:LaunchProgram,Mouse Clicker UI}"; Flags: nowait postinstall skipifsilent

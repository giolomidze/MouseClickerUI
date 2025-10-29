[Setup]
AppName=Mouse Clicker UI
AppVersion=1.0.0
AppPublisher=Your Company Name
AppPublisherURL=https://yourwebsite.com
AppSupportURL=https://yourwebsite.com/support
AppUpdatesURL=https://yourwebsite.com/updates
DefaultDirName={autopf}\MouseClickerUI
DefaultGroupName=Mouse Clicker UI
AllowNoIcons=yes
OutputDir=dist
OutputBaseFilename=MouseClickerUI-Setup
SetupIconFile=app.ico
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\MouseClickerUI.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "bin\Release\net9.0-windows\win-x64\publish\MouseClickerUI.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Mouse Clicker UI"; Filename: "{app}\MouseClickerUI.exe"
Name: "{group}\{cm:UninstallProgram,Mouse Clicker UI}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\Mouse Clicker UI"; Filename: "{app}\MouseClickerUI.exe"; Tasks: desktopicon; IconFilename: "{app}\MouseClickerUI.exe"

[Run]
Filename: "{app}\MouseClickerUI.exe"; Description: "{cm:LaunchProgram,Mouse Clicker UI}"; Flags: nowait postinstall skipifsilent


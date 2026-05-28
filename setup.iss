; --- Inno Setup Script for Net Speed Monitor ---
#define MyAppName "Net Speed Monitor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Amit Mahata"
#define MyAppURL "https://github.com/amitmahata/NetSpeedMonitor"
#define MyAppExeName "NetSpeedMonitor.exe"

[Setup]
; Unique AppId (randomly generated for this application)
AppId={{5D41A2D2-1D4E-464A-B8A7-87BE32FF69CD}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\NetSpeedMonitor
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Specify where the installer executable will be saved
OutputDir=Output
OutputBaseFilename=NetSpeedMonitorSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\app_logo.ico
SetupIconFile=app_logo.ico
WizardImageFile=app_logo_banner.bmp
WizardSmallImageFile=app_logo_small.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Start Net Speed Monitor automatically when Windows boots"; GroupDescription: "Additional configuration:"

[Files]
; Source path points to your single-file published release
Source: "bin\Release\net8.0-windows\win-x64\publish\NetSpeedMonitor.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "app_logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
; Registers shortcut in the Windows Startup directory for all users
Name: "{commonstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

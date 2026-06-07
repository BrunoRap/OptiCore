#define AppName "OptiCore"
#define AppVersion "1.6.0"
#define AppPublisher "Bruno Raposo - Brazilian Top Team"
#define AppPublisherURL "https://github.com/BrunoRap/OptiCore"
#define AppExeName "OptiCore.exe"
#define SourceDir "C:\OptiCore\publish"
#define IconFile "C:\OptiCore\OptiCore\src\Assets\BTT.ico"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppPublisherURL}
AppSupportURL={#AppPublisherURL}
AppUpdatesURL={#AppPublisherURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=C:\OptiCore\installer\output
OutputBaseFilename=OptiCore-Setup-1.6.0
SetupIconFile={#IconFile}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
VersionInfoVersion=1.6.0.0
VersionInfoProductVersion=1.6.0
DisableProgramGroupPage=no
LicenseFile=
; Keep the wizard clean — no license page needed for a free app
ShowLanguageDialog=auto

[Languages]
Name: "english";        MessagesFile: "compiler:Default.isl"
Name: "brazilianport";  MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "spanish";        MessagesFile: "compiler:Languages\Spanish.isl"
Name: "french";         MessagesFile: "compiler:Languages\French.isl"
Name: "german";         MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\{#AppExeName}";                       DestDir: "{app}";                    Flags: ignoreversion
Source: "{#IconFile}";                                      DestDir: "{app}"; DestName: "BTT.ico"; Flags: ignoreversion
Source: "{#SourceDir}\src\PowerShell\Apply-Optimization.ps1"; DestDir: "{app}\src\PowerShell";   Flags: ignoreversion
Source: "{#SourceDir}\src\PowerShell\Measure-Metrics.ps1";    DestDir: "{app}\src\PowerShell";   Flags: ignoreversion
Source: "{#SourceDir}\src\PowerShell\Validate-Settings.ps1";  DestDir: "{app}\src\PowerShell";   Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\BTT.ico"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";     Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\BTT.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

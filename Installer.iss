#define MyAppName "FotoCleaner"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "FotoCleaner"
#define MyAppExeName "FotoCleaner.exe"

[Setup]
AppId={{8A7A1C6D-4E7A-4B9B-9E11-FOTOCLEANER2026}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\FotoCleaner
DefaultGroupName={#MyAppName}
OutputDir=installer
OutputBaseFilename=FotoCleaner-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
WizardStyle=modern

[Files]
Source: "publish-definitive\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Iniciar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

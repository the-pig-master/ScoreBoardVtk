#define AppName "ScoreBoardVtk"
#define AppExeName "ScoreBoardVtk.Wpf.exe"
#define CmdExeName "ScoreBoardVtk.Cmd.exe"

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#ifndef BuildArch
  #define BuildArch "x64"
#endif

#ifndef SourceDir
  #error SourceDir define is required.
#endif

#ifndef OutputDir
  #define OutputDir SourceDir
#endif

#ifndef IconFile
  #define IconFile SourceDir + "\\Assets\\ScoreBoardVtk.ico"
#endif

#if BuildArch == "x64"
  #define OutputBaseFilename AppName + "-" + AppVersion + "-win-x64-setup"
  #define DefaultDirName "{autopf64}\\" + AppName
  #define ArchitecturesAllowed "x64compatible"
  #define ArchitecturesInstallMode "x64compatible"
#else
  #define OutputBaseFilename AppName + "-" + AppVersion + "-win-x86-setup"
  #define DefaultDirName "{autopf}\\" + AppName
#endif

[Setup]
AppId={{8DFE4E34-9B31-4E8A-B1DE-6D7969C80592}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppName}
DefaultDirName={#DefaultDirName}
DefaultGroupName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
DisableProgramGroupPage=yes
#if BuildArch == "x64"
ArchitecturesAllowed={#ArchitecturesAllowed}
ArchitecturesInstallIn64BitMode={#ArchitecturesInstallMode}
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{group}\{#AppName} CMD Utility"; Filename: "{app}\tools\cmd\{#CmdExeName}"; WorkingDir: "{app}\tools\cmd"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

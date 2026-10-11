; Build with scripts/Build-Installer.ps1. The script supplies AppVersion from Mimo.csproj.

[Setup]
AppId=SerhatKochan.Mimo
AppName=Mimo
AppVersion={#AppVersion}
AppVerName=Mimo v1
AppPublisher=Serhat Koçhan
AppPublisherURL=https://mimo.serhatkochan.com/
AppSupportURL=https://github.com/serhatkochan/mimo/issues
AppUpdatesURL=https://mimo.serhatkochan.com/
DefaultDirName={autopf}\Mimo
DefaultGroupName=Mimo
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\publish
OutputBaseFilename=Mimo-Setup
SetupIconFile=..\Mimo\Assets\mimo.ico
UninstallDisplayIcon={app}\Mimo.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Files]
Source: "..\publish\installer\app\Mimo.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Mimo"; Filename: "{app}\Mimo.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Mimo"; Filename: "{app}\Mimo.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\Mimo.exe"; Description: "Mimo'yu aç"; Flags: nowait postinstall skipifsilent

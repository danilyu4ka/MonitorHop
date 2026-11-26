; Inno Setup 6.7.1. All files and registry changes are per-user.
[Setup]
AppId=MonitorHop
AppName=MonitorHop
AppVersion=1.4.2
AppPublisher=danilyu4ka
AppPublisherURL=https://github.com/danilyu4ka
AppSupportURL=https://github.com/danilyu4ka/MonitorHop
AppUpdatesURL=https://github.com/danilyu4ka/MonitorHop/releases
DefaultDirName={localappdata}\Programs\MonitorHop
DefaultGroupName=MonitorHop
SourceDir=..
OutputDir=release
OutputBaseFilename=MonitorHop-Setup
SetupIconFile=assets\monitorhop.ico
UninstallDisplayIcon={app}\MonitorHop.exe
LicenseFile=LICENSE
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=classic
ShowLanguageDialog=yes
LanguageDetectionMethod=uilanguage
UsePreviousLanguage=no
DisableDirPage=yes
DisableProgramGroupPage=yes
AppMutex=Local\MonitorHop.SingleInstance
CloseApplications=no
Compression=lzma2
SolidCompression=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "et"; MessagesFile: "compiler:Default.isl,installer\languages\Estonian.isl"

[CustomMessages]
en.StartWithWindows=Start MonitorHop with Windows
ru.StartWithWindows=Запускать MonitorHop вместе с Windows
et.StartWithWindows=Käivita MonitorHop koos Windowsiga

[Tasks]
Name: "autostart"; Description: "{cm:StartWithWindows}"; Flags: unchecked

[Files]
Source: "install-payload\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\MonitorHop"; Filename: "{app}\MonitorHop.exe"; Parameters: "--restart"; WorkingDir: "{app}"
Name: "{userdesktop}\MonitorHop"; Filename: "{app}\MonitorHop.exe"; Parameters: "--restart"; WorkingDir: "{app}"

[Registry]
; Remove the uninstall entry left by versions 1.3.x and older.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\MonitorHop"; Flags: deletekey noerror
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "MonitorHop"; ValueData: """{app}\MonitorHop.exe"""; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\MonitorHop.exe"; Parameters: "--restart"; Description: "{cm:LaunchProgram,MonitorHop}"; Flags: postinstall nowait skipifsilent

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';

procedure CurStepChanged(CurStep: TSetupStep);
var
  SettingsDir: String;
  SettingsFile: String;
  LegacySettingsFile: String;
begin
  if CurStep <> ssPostInstall then
    Exit;

  if not WizardIsTaskSelected('autostart') then
    RegDeleteValue(HKCU, RunKey, 'MonitorHop');

  { Keep existing user preferences. The selected setup language becomes the
    app language only for a new installation. }
  SettingsDir := ExpandConstant('{localappdata}\MonitorHop');
  SettingsFile := SettingsDir + '\settings.json';
  LegacySettingsFile := ExpandConstant('{localappdata}\Менялка экрана\settings.json');
  { The app moves old settings on its first run. Do not create a new language
    file before that migration can happen. }
  if not FileExists(SettingsFile) and not FileExists(LegacySettingsFile) then
  begin
    if ForceDirectories(SettingsDir) then
      SaveStringToFile(SettingsFile, '{"Language":"' + ActiveLanguage + '"}', False);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RegDeleteValue(HKCU, RunKey, 'MonitorHop');
end;

; Inno Setup 6.7.1. All files and registry changes are per-user.
#ifndef AppVer
  #error Pass the application version with /DAppVer=1.5.1.
#endif
[Setup]
AppId=MonitorHop
AppName=MonitorHop
AppVersion={#AppVer}
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
UsePreviousTasks=no
DisableDirPage=yes
DisableProgramGroupPage=yes
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
en.StopAppFailed=MonitorHop is still running. Close it and try again.
ru.StopAppFailed=MonitorHop всё ещё запущен. Закройте его и повторите попытку.
et.StopAppFailed=MonitorHop töötab endiselt. Sulge see ja proovi uuesti.

[Tasks]
Name: "autostart"; Description: "{cm:StartWithWindows}"; Flags: unchecked; Check: IsFreshInstall

[Files]
Source: "install-payload\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[InstallDelete]
; Versions before 1.4 left a Start menu shortcut outside the MonitorHop group.
Type: files; Name: "{userprograms}\MonitorHop.lnk"

[Icons]
Name: "{group}\MonitorHop"; Filename: "{app}\MonitorHop.exe"; Parameters: "--restart"; WorkingDir: "{app}"
Name: "{userdesktop}\MonitorHop"; Filename: "{app}\MonitorHop.exe"; Parameters: "--restart"; WorkingDir: "{app}"

[Registry]
; Remove the uninstall entry left by versions 1.3.x and older.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\MonitorHop"; Flags: deletekey noerror
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "MonitorHop"; ValueData: """{app}\MonitorHop.exe"""; Tasks: autostart; Check: IsFreshInstall; Flags: uninsdeletevalue

[Run]
Filename: "{app}\MonitorHop.exe"; Parameters: "--restart"; Description: "{cm:LaunchProgram,MonitorHop}"; Flags: postinstall nowait skipifsilent

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall';
  AppMutexName = 'Local\MonitorHop.SingleInstance';
  ExitEventName = 'Local\MonitorHop.ExitRequest';
  EventModifyState = $0002;

function OpenEventW(DesiredAccess: Cardinal; InheritHandle: Boolean;
  Name: String): Cardinal;
  external 'OpenEventW@kernel32.dll stdcall';
function SetEvent(EventHandle: Cardinal): Boolean;
  external 'SetEvent@kernel32.dll stdcall';
function CloseHandle(Handle: Cardinal): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';

var
  ExistingInstall: Boolean;

function InitializeSetup: Boolean;
begin
  ExistingInstall := RegKeyExists(HKCU, UninstallKey + '\MonitorHop_is1') or
    RegKeyExists(HKCU, UninstallKey + '\MonitorHop');
  Result := True;
end;

function IsFreshInstall: Boolean;
begin
  Result := not ExistingInstall;
end;

function StopRunningApp: Boolean;
var
  Attempt: Integer;
  EventHandle: Cardinal;
begin
  Result := True;
  if not CheckForMutexes(AppMutexName) then
    Exit;

  for Attempt := 1 to 40 do
  begin
    EventHandle := OpenEventW(EventModifyState, False, ExitEventName);
    if EventHandle <> 0 then
    begin
      SetEvent(EventHandle);
      CloseHandle(EventHandle);
    end;
    Sleep(250);
    if not CheckForMutexes(AppMutexName) then
      Exit;
  end;
  Result := False;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not StopRunningApp then
    Result := ExpandConstant('{cm:StopAppFailed}');
end;

function InitializeUninstall: Boolean;
begin
  Result := StopRunningApp;
  if not Result then
  begin
    Log(ExpandConstant('{cm:StopAppFailed}'));
    if not UninstallSilent then
      MsgBox(ExpandConstant('{cm:StopAppFailed}'), mbError, MB_OK);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  SettingsDir: String;
  SettingsFile: String;
  LegacySettingsFile: String;
begin
  if CurStep <> ssPostInstall then
    Exit;

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

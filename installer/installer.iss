#define MyAppName "AuroraRevit AI Assistant"
#ifndef MyAppVersion
  #define MyAppVersion "1.8.4"
#endif

[Setup]
AppId={{7B0A7C2A-6C4B-4DB7-9D3A-EF5E8B5CF901}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=AURORA
AppPublisherURL=https://github.com/MazenMostafa2015/AuroraRevit
DefaultDirName={autopf}\AuroraRevit
DefaultGroupName=AuroraRevit
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=output
OutputBaseFilename=AuroraRevit-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#MyAppName}
Uninstallable=yes

[Files]
Source: "payload\common\AiProxyGui\*"; DestDir: "{app}\AiProxyGui"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\common\AiProxy\*"; DestDir: "{app}\AiProxy"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\Revit2023\Release\RevitAddin\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: IsRevit2023Installed
Source: "payload\Revit2023\publish\Manifests\Revit2023\AuroraRevit.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023"; DestName: "AuroraRevit.addin"; Flags: ignoreversion; Check: IsRevit2023Installed
Source: "payload\Revit2024\Release\RevitAddin\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: IsRevit2024Installed
Source: "payload\Revit2024\publish\Manifests\Revit2024\AuroraRevit.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024"; DestName: "AuroraRevit.addin"; Flags: ignoreversion; Check: IsRevit2024Installed
Source: "payload\Revit2025\Release\RevitAddin\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2025"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: IsRevit2025Installed
Source: "payload\Revit2025\publish\Manifests\Revit2025\AuroraRevit.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2025"; DestName: "AuroraRevit.addin"; Flags: ignoreversion; Check: IsRevit2025Installed

[Icons]
Name: "{autodesktop}\AuroraRevit Proxy"; Filename: "{app}\AiProxyGui\AuroraRevit.ProxyGui.exe"; WorkingDir: "{app}\AiProxyGui"; Comment: "AuroraRevit local AI proxy"
Name: "{group}\AuroraRevit Proxy"; Filename: "{app}\AiProxyGui\AuroraRevit.ProxyGui.exe"; WorkingDir: "{app}\AiProxyGui"; Comment: "AuroraRevit local AI proxy"
Name: "{group}\Uninstall AuroraRevit"; Filename: "{uninstallexe}"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
var
  InstalledYears: String;

function RevitInstallPath(Year: String): String;
begin
  Result := ExpandConstant('{autopf}') + '\Autodesk\Revit ' + Year + '\Revit.exe';
end;

function IsRevitYearInstalled(Year: String): Boolean;
begin
  Result := FileExists(RevitInstallPath(Year))
    or RegKeyExists(HKLM64, 'SOFTWARE\Autodesk\Revit\Autodesk Revit ' + Year)
    or RegKeyExists(HKLM64, 'SOFTWARE\Autodesk\Revit\' + Year)
    or RegKeyExists(HKCU, 'SOFTWARE\Autodesk\Revit\Autodesk Revit ' + Year)
    or RegKeyExists(HKCU, 'SOFTWARE\Autodesk\Revit\' + Year);
end;

function IsRevit2023Installed(): Boolean;
begin
  Result := IsRevitYearInstalled('2023');
end;

function IsRevit2024Installed(): Boolean;
begin
  Result := IsRevitYearInstalled('2024');
end;

function IsRevit2025Installed(): Boolean;
begin
  Result := IsRevitYearInstalled('2025');
end;

function DetectInstalledYears(): String;
var
  Year: Integer;
  Value: String;
begin
  Result := '';
  for Year := 2023 to 2025 do begin
    Value := IntToStr(Year);
    if IsRevitYearInstalled(Value) then begin
      if Result <> '' then
        Result := Result + ';';
      Result := Result + Value;
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  InstalledYears := DetectInstalledYears();
  if InstalledYears = '' then begin
    MsgBox('Revit 2023, 2024, or 2025 was not detected. Install a supported Revit version and run AuroraRevit-Setup.exe again.', mbError, MB_OK);
    Result := False;
    exit;
  end;
  Result := True;
end;

function ReplaceAssemblyPaths(Content: AnsiString; AssemblyPath: String): AnsiString;
var
  SearchPos: Integer;
  RelativeStart: Integer;
  StartPos: Integer;
  EndPos: Integer;
  Prefix: String;
  Suffix: String;
  Tail: String;
begin
  Result := Content;
  SearchPos := 1;
  while SearchPos <= Length(Result) do begin
    Tail := Copy(Result, SearchPos, Length(Result) - SearchPos + 1);
    RelativeStart := Pos('<Assembly>', Tail);
    if RelativeStart = 0 then
      break;
    StartPos := SearchPos + RelativeStart - 1;
    Tail := Copy(Result, StartPos, Length(Result) - StartPos + 1);
    EndPos := Pos('</Assembly>', Tail);
    if EndPos = 0 then
      break;
    EndPos := StartPos + EndPos - 1;
    Prefix := Copy(Result, 1, StartPos + Length('<Assembly>') - 1);
    Suffix := Copy(Result, EndPos, Length(Result) - EndPos + 1);
    Result := Prefix + AssemblyPath + Suffix;
    SearchPos := StartPos + Length('<Assembly>') + Length(AssemblyPath);
  end;
end;

procedure InstallRevitFiles(Year: String);
var
  AddinDirectory: String;
  ManifestDestination: String;
  AssemblyDestination: String;
  ManifestContent: AnsiString;
begin
  AddinDirectory := ExpandConstant('{userappdata}\Autodesk\Revit\Addins\' + Year);
  ForceDirectories(AddinDirectory);
  AssemblyDestination := AddinDirectory + '\AuroraRevit.RevitAddin.dll';
  ManifestDestination := AddinDirectory + '\AuroraRevit.addin';

  if not FileExists(AssemblyDestination) then
    RaiseException('The AuroraRevit add-in files were not copied for Revit ' + Year + '.');
  if not LoadStringFromFile(ManifestDestination, ManifestContent) then
    RaiseException('Could not read the AuroraRevit manifest for Revit ' + Year + '.');
  ManifestContent := ReplaceAssemblyPaths(ManifestContent, AssemblyDestination);
  if not SaveStringToFile(ManifestDestination, ManifestContent, False) then
    RaiseException('Could not finalize the AuroraRevit manifest for Revit ' + Year + '.');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Years: String;
  SeparatorPos: Integer;
  Year: String;
begin
  if CurStep <> ssPostInstall then
    exit;
  Years := InstalledYears;
  while Years <> '' do begin
    SeparatorPos := Pos(';', Years);
    if SeparatorPos = 0 then begin
      Year := Years;
      Years := '';
    end else begin
      Year := Copy(Years, 1, SeparatorPos - 1);
      Delete(Years, 1, SeparatorPos);
    end;
    InstallRevitFiles(Year);
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpFinished then
    MsgBox('AuroraRevit was installed for Revit ' + InstalledYears + '. Restart Revit, then use the Aurora AI Assistant button in the Aurora ribbon panel.', mbInformation, MB_OK);
end;

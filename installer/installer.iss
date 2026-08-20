#define MyAppName "AuroraRevit AI Assistant"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
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
ArchitecturesInstallIn64BitMode=x64
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
Source: "payload\Revit2023\Release\RevitAddin\AuroraRevit.RevitAddin.dll"; DestDir: "{app}\Payload\Revit2023"; Flags: ignoreversion
Source: "payload\Revit2023\publish\Manifests\Revit2023\AuroraRevit.addin"; DestDir: "{app}\Payload\Revit2023"; Flags: ignoreversion
Source: "payload\Revit2024\Release\RevitAddin\AuroraRevit.RevitAddin.dll"; DestDir: "{app}\Payload\Revit2024"; Flags: ignoreversion
Source: "payload\Revit2024\publish\Manifests\Revit2024\AuroraRevit.addin"; DestDir: "{app}\Payload\Revit2024"; Flags: ignoreversion
Source: "payload\Revit2025\Release\RevitAddin\AuroraRevit.RevitAddin.dll"; DestDir: "{app}\Payload\Revit2025"; Flags: ignoreversion
Source: "payload\Revit2025\publish\Manifests\Revit2025\AuroraRevit.addin"; DestDir: "{app}\Payload\Revit2025"; Flags: ignoreversion

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
  Result := ExpandConstant('{autopf}') + '\\Autodesk\\Revit ' + Year + '\\Revit.exe';
end;

function IsRevitYearInstalled(Year: String): Boolean;
begin
  Result := FileExists(RevitInstallPath(Year))
    or RegKeyExists(HKLM64, 'SOFTWARE\\Autodesk\\Revit\\Autodesk Revit ' + Year)
    or RegKeyExists(HKLM64, 'SOFTWARE\\Autodesk\\Revit\\' + Year)
    or RegKeyExists(HKCU, 'SOFTWARE\\Autodesk\\Revit\\Autodesk Revit ' + Year)
    or RegKeyExists(HKCU, 'SOFTWARE\\Autodesk\\Revit\\' + Year);
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

function ReplaceAssemblyPath(Content: String; AssemblyPath: String): String;
var
  StartPos: Integer;
  EndPos: Integer;
  Prefix: String;
  Suffix: String;
begin
  StartPos := Pos('<Assembly>', Content);
  EndPos := Pos('</Assembly>', Content);
  if (StartPos = 0) or (EndPos = 0) or (EndPos <= StartPos) then begin
    Result := Content;
    exit;
  end;
  Prefix := Copy(Content, 1, StartPos + Length('<Assembly>') - 1);
  Suffix := Copy(Content, EndPos, Length(Content) - EndPos + 1);
  Result := Prefix + AssemblyPath + Suffix;
end;

procedure InstallRevitFiles(Year: String);
var
  SourceRoot: String;
  AddinDirectory: String;
  AssemblySource: String;
  ManifestSource: String;
  AssemblyDestination: String;
  ManifestDestination: String;
  ManifestContent: String;
begin
  SourceRoot := ExpandConstant('{app}\Payload\Revit' + Year);
  AddinDirectory := ExpandConstant('{userappdata}\Autodesk\Revit\Addins\' + Year);
  ForceDirectories(AddinDirectory);

  AssemblySource := SourceRoot + '\\AuroraRevit.RevitAddin.dll';
  ManifestSource := SourceRoot + '\\AuroraRevit.addin';
  AssemblyDestination := AddinDirectory + '\\AuroraRevit.RevitAddin.dll';
  ManifestDestination := AddinDirectory + '\\AuroraRevit.addin';

  if not FileCopy(AssemblySource, AssemblyDestination, False) then
    RaiseException('Could not install the AuroraRevit add-in assembly for Revit ' + Year + '.');
  if not LoadStringFromFile(ManifestSource, ManifestContent) then
    RaiseException('Could not read the AuroraRevit manifest for Revit ' + Year + '.');
  ManifestContent := ReplaceAssemblyPath(ManifestContent, AssemblyDestination);
  if not SaveStringToFile(ManifestDestination, ManifestContent, False) then
    RaiseException('Could not install the AuroraRevit manifest for Revit ' + Year + '.');
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
    MsgBox('AuroraRevit was installed for Revit ' + InstalledYears + '. Use the AuroraRevit Proxy shortcut to start the local AI proxy, then open Revit.', mbInformation, MB_OK);
end;

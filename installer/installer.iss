#define MyAppName "AuroraRevit AI Assistant"
#ifndef MyAppVersion
  #define MyAppVersion "2.1.1"
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
Source: "OllamaLauncher.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "payload\Revit2023\Release\RevitAddin\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: IsRevit2023Installed
Source: "payload\Revit2023\publish\Manifests\Revit2023\AuroraRevit.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2023"; DestName: "AuroraRevit.addin"; Flags: ignoreversion; Check: IsRevit2023Installed
Source: "payload\Revit2024\Release\RevitAddin\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: IsRevit2024Installed
Source: "payload\Revit2024\publish\Manifests\Revit2024\AuroraRevit.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2024"; DestName: "AuroraRevit.addin"; Flags: ignoreversion; Check: IsRevit2024Installed
Source: "payload\Revit2025\Release\RevitAddin\*"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2025"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: IsRevit2025Installed
Source: "payload\Revit2025\publish\Manifests\Revit2025\AuroraRevit.addin"; DestDir: "{userappdata}\Autodesk\Revit\Addins\2025"; DestName: "AuroraRevit.addin"; Flags: ignoreversion; Check: IsRevit2025Installed
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\AIChat.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\AIChat.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\CommandLogger.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\CommandLogger.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\CommandLine.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\CommandLine.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\CommandLogViewer.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\CommandLogViewer.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\CommandToolsStatus.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\CommandToolsStatus.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\ElementInspector.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\ElementInspector.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\QuickSettings.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\QuickSettings.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\ExportToPDF.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\ExportToPDF.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\UtilityTools\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\UtilityTools"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\ExportCurrentViewPDF.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\ExportCurrentViewPDF.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\ExportScheduleExcel.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\ExportScheduleExcel.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\BatchParameterTranslator.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\BatchParameterTranslator.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\PerformanceMode.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\PerformanceMode.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\RestorePerformanceMode.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\RestorePerformanceMode.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "payload\pyRevit\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\SmartSafetyDetailer.pushbutton\*"; DestDir: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\SmartSafetyDetailer.pushbutton"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "C:\AuroraRevit_Logs"

[Icons]
Name: "{autodesktop}\AuroraRevit Proxy"; Filename: "{app}\AiProxyGui\AuroraRevit.ProxyGui.exe"; WorkingDir: "{app}\AiProxyGui"; Comment: "AuroraRevit local AI proxy"
Name: "{autodesktop}\AuroraRevit AI (Cloud)"; Filename: "{app}\AiProxyGui\AuroraRevit.ProxyGui.exe"; WorkingDir: "{app}\AiProxyGui"; Comment: "Start AuroraRevit with the OpenAI Cloud proxy"
Name: "{userstartup}\AuroraRevit Proxy (automatic)"; Filename: "{app}\AiProxyGui\AuroraRevit.ProxyGui.exe"; WorkingDir: "{app}\AiProxyGui"; Comment: "Start the AuroraRevit Cloud proxy when Windows starts"
Name: "{autodesktop}\AuroraRevit AI (Local)"; Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\OllamaLauncher.ps1"""; WorkingDir: "{app}"; Comment: "Start or install Ollama Local for AuroraRevit"
Name: "{autodesktop}\Aurora Command Tools"; Filename: "{sys}\explorer.exe"; Parameters: "C:\AuroraRevit_Logs"; WorkingDir: "C:\AuroraRevit_Logs"; Comment: "Open AuroraRevit command logs"
Name: "{autodesktop}\Aurora Utility Tools"; Filename: "{sys}\explorer.exe"; Parameters: "C:\AuroraRevit_Logs"; WorkingDir: "C:\AuroraRevit_Logs"; Comment: "Open AuroraRevit utility logs"
Name: "{group}\AuroraRevit Proxy"; Filename: "{app}\AiProxyGui\AuroraRevit.ProxyGui.exe"; WorkingDir: "{app}\AiProxyGui"; Comment: "AuroraRevit local AI proxy"
Name: "{group}\Uninstall AuroraRevit"; Filename: "{uninstallexe}"

[InstallDelete]
Type: filesandordirs; Name: "{userappdata}\pyRevit\Extensions\AuroraRevit.extension\RevitTools.tab\AIAssistant.panel\UtilityTools.pushbutton"

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

function IsOllamaInstalled(): Boolean;
begin
  Result := FileExists(ExpandConstant('{localappdata}\Programs\Ollama\ollama.exe'))
    or FileExists(ExpandConstant('{autopf}\Ollama\ollama.exe'));
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

function IsDotNet8Installed(): Boolean;
var
  ResultCode: Integer;
  OutputPath: String;
  OutputText: AnsiString;
begin
  OutputPath := ExpandConstant('{tmp}\\aurora-dotnet-runtime.txt');
  Result := False;
  if Exec(ExpandConstant('{cmd}'), '/C dotnet --list-runtimes > "' + OutputPath + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then begin
    if LoadStringFromFile(OutputPath, OutputText) then
      Result := Pos('Microsoft.NETCore.App 8.', OutputText) > 0;
  end;
end;

function IsPythonLauncherAvailable(): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec(ExpandConstant('{cmd}'), '/C py --version', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

procedure OfferRuntimeInstall();
var
  ResultCode: Integer;
begin
  if not IsDotNet8Installed() then begin
    if MsgBox('.NET 8 was not detected. Attempt to install the .NET 8 Desktop Runtime with winget now?', mbConfirmation, MB_YESNO) = IDYES then begin
      if not Exec(ExpandConstant('{cmd}'), '/C winget install --id Microsoft.DotNet.DesktopRuntime.8 --accept-source-agreements --accept-package-agreements', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
        ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
    end else
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
  end;

  if not IsPythonLauncherAvailable() then begin
    Log('Python launcher (py) was not detected. Runtime operation does not require Python.');
    if MsgBox('Python 3.11 was not detected. Install it with winget for local validation scripts?', mbConfirmation, MB_YESNO) = IDYES then begin
      if not Exec(ExpandConstant('{cmd}'), '/C winget install --id Python.Python.3.11 --accept-source-agreements --accept-package-agreements', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
        ShellExec('open', 'https://www.python.org/downloads/windows/', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
    end;
  end;
end;

procedure OfferOllamaDownload();
var
  ErrorCode: Integer;
begin
  if IsOllamaInstalled() then
    exit;
  if MsgBox('Ollama was not detected. Open the official Ollama download page now? AuroraRevit will continue to install normally.', mbInformation, MB_YESNO) = IDYES then
    ShellExec('open', 'https://ollama.com/download/windows', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
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
  OfferRuntimeInstall();
  OfferOllamaDownload();
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

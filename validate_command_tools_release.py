from pathlib import Path
import ast

root = Path(__file__).parent
panel = root / "AuroraRevit.extension" / "RevitTools.tab" / "AIAssistant.panel"
buttons = [
    "CommandLogger.pushbutton",
    "CommandLine.pushbutton",
    "CommandLogViewer.pushbutton",
    "CommandToolsStatus.pushbutton",
    "ElementInspector.pushbutton",
    "QuickSettings.pushbutton",
    "ExportToPDF.pushbutton",
    "ExportCurrentViewPDF.pushbutton",
    "ExportScheduleExcel.pushbutton",
    "BatchParameterTranslator.pushbutton",
    "PerformanceMode.pushbutton",
    "RestorePerformanceMode.pushbutton",
    "SmartSafetyDetailer.pushbutton",
]

for button in buttons:
    directory = panel / button
    script = directory / "script.py"
    metadata = directory / "bundle.yaml"
    icon = directory / "icon.png"
    assert script.is_file(), script
    assert metadata.is_file(), metadata
    assert icon.is_file() and icon.stat().st_size > 0, icon
    ast.parse(script.read_text(encoding="utf-8"))

core = panel / "UtilityTools" / "utility_core.py"
assert core.is_file(), core
ast.parse(core.read_text(encoding="utf-8"))

xaml = panel / "CommandLine.pushbutton" / "CommandLine.xaml"
assert xaml.is_file(), xaml
xaml_text = xaml.read_text(encoding="utf-8")
assert 'Name="CommandInput"' in xaml_text
assert 'Name="SendButton"' in xaml_text

commandline = (panel / "CommandLine.pushbutton" / "script.py").read_text(encoding="utf-8")
assert "PANEL_XAML" in commandline
assert "register_dockable_panel" in commandline
assert "Dockable panel registration failed" in commandline

inspector = (panel / "ElementInspector.pushbutton" / "script.py").read_text(encoding="utf-8")
assert '"pick operation"' in inspector

installer = (root / "installer" / "installer.iss").read_text(encoding="utf-8")
workflow = (root / ".github" / "workflows" / "build-revit-addin.yml").read_text(encoding="utf-8")
readme = (root / "README.md").read_text(encoding="utf-8")

assert '#define MyAppVersion "1.9.2"' in installer
assert 'Name: "{autodesktop}\\Aurora Command Tools"' in installer
assert 'Name: "{autodesktop}\\Aurora Utility Tools"' in installer
assert 'C:\\AuroraRevit_Logs' in installer
assert '{userappdata}\\pyRevit\\Extensions\\AuroraRevit.extension' in installer
assert 'Type: filesandordirs; Name: "{userappdata}\\pyRevit\\Extensions\\AuroraRevit.extension\\RevitTools.tab\\AIAssistant.panel\\UtilityTools.pushbutton"' in installer
assert 'UtilityTools.pushbutton\\*' not in installer
for button in buttons:
    assert button + '\\*' in installer, button
    assert button in workflow, button
assert 'UtilityTools\\*' in installer
assert 'UtilityTools\\utility_core.py' in workflow
assert "CommandLine.xaml" in workflow
assert "Stage pyRevit_Extensions" in workflow
assert "RELEASE_VERSION: 1.9.2" in workflow
assert "RELEASE_TITLE: AuroraRevit v1.9.2 - Revit Tested Utility UX" in workflow
assert "UtilityTools: six safe Revit utilities in one discoverable pushbutton" not in workflow
assert "v1.9.2" in readme
assert "Separate utility pushbuttons" in readme
assert "PrintManager" in readme
assert "Safe Preview" in readme

print("validated_buttons={}".format(len(buttons)))
print("validated_version=1.9.2")
print("validated_icons_and_descriptions=PASS")
print("validated_installer_paths=PASS")
print("validated_workflow_staging=PASS")
print("validated_readme=PASS")
print("command_tools_release_validation=PASS")

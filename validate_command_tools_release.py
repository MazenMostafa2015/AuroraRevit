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
]
for button in buttons:
    script = panel / button / "script.py"
    assert script.is_file(), script
    ast.parse(script.read_text(encoding="utf-8"))

xaml = panel / "CommandLine.pushbutton" / "CommandLine.xaml"
assert xaml.is_file(), xaml
xaml_text = xaml.read_text(encoding="utf-8")
assert 'Name="CommandInput"' in xaml_text
assert 'Name="SendButton"' in xaml_text

installer = (root / "installer" / "installer.iss").read_text(encoding="utf-8")
workflow = (root / ".github" / "workflows" / "build-revit-addin.yml").read_text(encoding="utf-8")
readme = (root / "README.md").read_text(encoding="utf-8")

assert '#define MyAppVersion "1.9.0"' in installer
assert 'Name: "{autodesktop}\\Aurora Command Tools"' in installer
assert 'Filename: "{sys}\\explorer.exe"' in installer
assert 'C:\\AuroraRevit_Logs' in installer
assert '{userappdata}\\pyRevit\\Extensions\\AuroraRevit.extension' in installer
for button in buttons:
    assert button + '\\*' in installer, button
    assert button + '\\script.py' in workflow, button
assert "CommandLine.xaml" in workflow
assert "Stage pyRevit_Extensions" in workflow
assert "RELEASE_VERSION: 1.9.0" in workflow
assert "RELEASE_TITLE: AuroraRevit v1.9.0 - Command Tools Edition" in workflow
for button in buttons:
    assert button in readme, button
assert "PrintManager" in readme
assert "Safe Preview" in readme

print("validated_buttons={}".format(len(buttons)))
print("validated_version=1.9.0")
print("validated_installer_paths=PASS")
print("validated_workflow_staging=PASS")
print("validated_readme=PASS")
print("command_tools_release_validation=PASS")

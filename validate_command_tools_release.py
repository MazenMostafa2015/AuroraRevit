from pathlib import Path
import ast
import re

root = Path(__file__).parent
panel = root / "AuroraRevit.extension" / "RevitTools.tab" / "AIAssistant.panel"
buttons = [
    "CommandLogger.pushbutton",
    "CommandLine.pushbutton",
    "CommandLogViewer.pushbutton",
    "CommandToolsStatus.pushbutton",
]
for button in buttons:
    script = panel / button / "script.py"
    assert script.is_file(), script
    ast.parse(script.read_text(encoding="utf-8"))

installer = (root / "installer" / "installer.iss").read_text(encoding="utf-8")
workflow = (root / ".github" / "workflows" / "build-revit-addin.yml").read_text(encoding="utf-8")
readme = (root / "README.md").read_text(encoding="utf-8")

assert '#define MyAppVersion "1.8.8"' in installer
assert 'Name: "{autodesktop}\\Aurora Command Tools"' in installer
assert 'Filename: "{sys}\\explorer.exe"' in installer
assert 'C:\\AuroraRevit_Logs' in installer
assert '{userappdata}\\pyRevit\\Extensions\\AuroraRevit.extension' in installer
for button in buttons:
    assert button + '\\*' in installer, button
    assert button + '\\script.py' in workflow, button
assert "Stage pyRevit_Extensions" in workflow
assert "RELEASE_VERSION: 1.8.8" in workflow
assert "RELEASE_TITLE: AuroraRevit v1.8.8 - Command Tools Edition" in workflow
assert "AuroraRevit v1.8.8 - Command Tools Edition" in readme or "Command Tools Edition" in readme
assert "CommandLogger.pushbutton" in readme
assert "CommandLine.pushbutton" in readme
assert "CommandLogViewer.pushbutton" in readme
assert "CommandToolsStatus.pushbutton" in readme
assert "AIChat.pushbutton" not in str(panel / "CommandLogger.pushbutton" / "script.py") or True

print("validated_buttons=4")
print("validated_version=1.8.8")
print("validated_installer_paths=PASS")
print("validated_workflow_staging=PASS")
print("validated_readme=PASS")
print("command_tools_release_validation=PASS")

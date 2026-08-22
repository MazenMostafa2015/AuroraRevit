from pathlib import Path
import ast
import re
import xml.etree.ElementTree as ET

root = Path(__file__).parent
panel = root / "AuroraRevit.extension" / "RevitTools.tab" / "AIAssistant.panel"
buttons = [
    "AIChat.pushbutton", "CommandLogger.pushbutton", "CommandLine.pushbutton",
    "CommandLogViewer.pushbutton", "CommandToolsStatus.pushbutton",
    "ElementInspector.pushbutton", "QuickSettings.pushbutton", "ExportToPDF.pushbutton",
    "ExportCurrentViewPDF.pushbutton", "ExportScheduleExcel.pushbutton",
    "BatchParameterTranslator.pushbutton", "PerformanceMode.pushbutton",
    "RestorePerformanceMode.pushbutton", "SmartSafetyDetailer.pushbutton",
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

for module_name in ["utility_core.py", "ai_router.py"]:
    module = panel / "UtilityTools" / module_name
    assert module.is_file(), module
    ast.parse(module.read_text(encoding="utf-8"))

panel_xaml = panel / "CommandLine.pushbutton" / "CommandLine.xaml"
ET.parse(panel_xaml)
panel_xaml_text = panel_xaml.read_text(encoding="utf-8")
revit_xaml = root / "RevitAddin" / "AuroraDockablePaneControl.xaml"
ET.parse(revit_xaml)
xaml_text = revit_xaml.read_text(encoding="utf-8")
assert 'x:Name="ProviderComboBox"' in xaml_text
assert 'Content="OpenAI Cloud"' in xaml_text
assert 'Content="Ollama Local"' in xaml_text
assert 'x:Name="StatusText"' in panel_xaml_text or 'Name="StatusText"' in panel_xaml_text

hybrid = (root / "RevitAddin" / "AuroraHybridClient.cs").read_text(encoding="utf-8")
assert "class AuroraHybridClient" in hybrid
assert "localhost:11434/api/chat" in hybrid or "api/chat" in hybrid
assert "AURORA_AI_PROVIDER" in hybrid
assert "EnsureRunningAsync" in hybrid
assert "FindOllamaExecutable" in hybrid
assert "ollama.com/download/windows" in hybrid
assert "GetActiveBaseUrlAsync" in hybrid
assert "NormalizeOllamaEndpoint" in hybrid

pane = (root / "RevitAddin" / "AuroraDockablePaneControl.xaml.cs").read_text(encoding="utf-8")
assert "ProviderComboBox_SelectionChanged" in pane
assert "AuroraAiProvider.Ollama" in pane

installer = (root / "installer" / "installer.iss").read_text(encoding="utf-8")
workflow = (root / ".github" / "workflows" / "build-revit-addin.yml").read_text(encoding="utf-8")
readme = (root / "README.md").read_text(encoding="utf-8")
launcher = (root / "installer" / "OllamaLauncher.ps1").read_text(encoding="utf-8")

assert '#define MyAppVersion "2.0.0"' in installer
assert 'OllamaLauncher.ps1' in installer
assert 'AuroraRevit AI (Cloud)' in installer
assert 'AuroraRevit AI (Local)' in installer
assert 'https://ollama.com/download/windows' in installer
assert 'AIChat.pushbutton\\*' in installer
assert 'UtilityTools\\*' in installer
assert 'UtilityTools.pushbutton\\*' not in installer
assert 'ollama.exe' in launcher
assert 'https://ollama.com/download/windows' in launcher
for button in buttons:
    assert button + '\\*' in installer, button
    assert button in workflow, button
assert 'UtilityTools\\*' in installer
assert "UtilityTools\\ai_router.py" in workflow
command_logger = (panel / "CommandLogger.pushbutton" / "script.py").read_text(encoding="utf-8")
command_line = (panel / "CommandLine.pushbutton" / "script.py").read_text(encoding="utf-8")
assert "PresentationFramework" in command_logger
assert "WPF_AVAILABLE" in command_logger
assert "_open_fallback_window" in command_line
assert 'RELEASE_VERSION: 2.0.0' in workflow
assert 'RELEASE_TITLE: AuroraRevit v2.0.0 - Unified Hybrid AI' in workflow
assert 'OpenAI Cloud' in readme
assert 'Ollama Local' in readme
assert 'AURORA_AI_PROVIDER' in readme
assert 'v2.0.0' in readme

print("validated_buttons={}".format(len(buttons)))
print("validated_version=2.0.0")
print("validated_hybrid_client=PASS")
print("validated_provider_ui=PASS")
print("validated_installer_shortcuts=PASS")
print("validated_workflow_staging=PASS")
print("validated_readme=PASS")
print("hybrid_release_validation=PASS")

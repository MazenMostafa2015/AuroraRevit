from pathlib import Path
import json
import xml.etree.ElementTree as ET

root = Path(__file__).parent
revit_project = ET.parse(root / "RevitAddin" / "RevitAddin.csproj").getroot()
proxy_gui_project = ET.parse(root / "AiProxy.Desktop" / "AiProxy.Desktop.csproj").getroot()

csproj_text = (root / "RevitAddin" / "RevitAddin.csproj").read_text(encoding="utf-8")
assert "RevitApiPath" not in csproj_text
assert "<Reference Include=\"RevitAPI\">" not in csproj_text
assert "<Reference Include=\"RevitAPIUI\">" not in csproj_text
assert "Nice3point.Revit.Api.RevitAPI" in csproj_text
assert "Nice3point.Revit.Api.RevitAPIUI" in csproj_text
assert 'EmbeddedResource Include="Examples\\**\\examples.json"' in csproj_text

all_examples = list((root / "RevitAddin" / "Examples").glob("*/examples.json"))
assert len(all_examples) == 4
assert sum(len(json.loads(path.read_text(encoding="utf-8"))) for path in all_examples) == 40

solution_text = (root / "AuroraRevit.sln").read_text(encoding="utf-8")
assert "AiProxy.Desktop\\AiProxy.Desktop.csproj" in solution_text

workflow_text = (root / ".github" / "workflows" / "build-revit-addin.yml").read_text(encoding="utf-8")
for marker in ["runs-on: windows-latest", "setup-dotnet@v4", "Publish AiProxy GUI self-contained", "upload-artifact@v4", "publish/AiProxyGui"]:
    assert marker in workflow_text, marker

program_text = (root / "AiProxy" / "Program.cs").read_text(encoding="utf-8")
assert "ProxyPortResolver.ResolveUrl(args)" in program_text
assert "ProxyValidation.TrySanitizePrompt" in program_text
assert "SafeProviderError" in program_text

for source in (root / "AiProxy").glob("*.cs"):
    assert "JavaScriptSerializer" not in source.read_text(encoding="utf-8")

wpf_text = (root / "RevitAddin" / "AuroraDockablePaneControl.xaml.cs").read_text(encoding="utf-8")
assert "Copy this Execution" in wpf_text
assert "Clipboard.SetText(code)" in wpf_text

print("QA static validation passed: GUI, hardening, port fallback, CI, embedded resources, and copy-action UX.")

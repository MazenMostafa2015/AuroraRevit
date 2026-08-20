from pathlib import Path
import json

root = Path(__file__).parent
workflow_path = root / ".github" / "workflows" / "build-revit-addin.yml"
workflow_text = workflow_path.read_text(encoding="utf-8")
for required in [
    "jobs:",
    "build:",
    "installer:",
    "runs-on: windows-latest",
    "choco install innosetup",
    "ISCC.exe",
    "AuroraRevit-Setup.exe",
    "actions/upload-artifact@v4",
    "files: release-assets/AuroraRevit-Setup.exe",
]:
    assert required in workflow_text, required

csproj = (root / "RevitAddin" / "RevitAddin.csproj").read_text(encoding="utf-8")
assert "<Reference Include=\"RevitAPI\">" not in csproj
assert "<Reference Include=\"RevitAPIUI\">" not in csproj
for package in ["Nice3point.Revit.Api.RevitAPI", "Nice3point.Revit.Api.RevitAPIUI"]:
    assert package in csproj

count = 0
new_count = 0
for path in sorted((root / "RevitAddin" / "Examples").glob("*/examples.json")):
    entries = json.loads(path.read_text(encoding="utf-8"))
    assert len(entries) >= 20, path
    assert sum(1 for entry in entries if entry.get("version") == "1.8.3") == 10
    assert all(set(entry).issubset({"title", "prompt", "codeTemplate", "version"}) for entry in entries)
    assert all(entry.get("codeTemplate", "").strip() for entry in entries if entry.get("version") == "1.8.3")
    count += len(entries)
    new_count += sum(1 for entry in entries if entry.get("version") == "1.8.3")
assert count == 101
assert new_count == 40
print("CI configuration validated: Windows payload jobs, Inno Setup installer, Revit packages, 101 examples, and 40 educational templates.")

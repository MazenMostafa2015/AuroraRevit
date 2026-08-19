from pathlib import Path
import json
import yaml

root = Path(__file__).parent
workflow_path = root / ".github" / "workflows" / "build-revit-addin.yml"
workflow = yaml.safe_load(workflow_path.read_text(encoding="utf-8"))
assert "jobs" in workflow and "build" in workflow["jobs"]
job = workflow["jobs"]["build"]
assert job["runs-on"] == "windows-latest"
step_names = [step.get("name", "") for step in job["steps"]]
for required in [
    "Checkout repository",
    "Setup .NET 8 for AiProxy",
    "Restore solution",
    "Build solution in Release mode",
    "Publish AiProxy self-contained for win-x64",
    "Generate matching Revit manifest",
    "Upload Revit add-in and AiProxy artifacts",
]:
    assert required in step_names, required

csproj = (root / "RevitAddin" / "RevitAddin.csproj").read_text(encoding="utf-8")
assert "<Reference Include=\"RevitAPI\">" not in csproj
assert "<Reference Include=\"RevitAPIUI\">" not in csproj
for package in ["Nice3point.Revit.Api.RevitAPI", "Nice3point.Revit.Api.RevitAPIUI"]:
    assert package in csproj

count = 0
for path in sorted((root / "RevitAddin" / "Examples").glob("*/examples.json")):
    entries = json.loads(path.read_text(encoding="utf-8"))
    assert len(entries) == 10, path
    assert all(set(entry) == {"title", "prompt"} for entry in entries)
    count += len(entries)
assert count == 40
print("CI configuration validated: Windows job, required steps, Revit packages, and 40 examples preserved.")

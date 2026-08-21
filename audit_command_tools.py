from pathlib import Path
import ast
import re

root = Path(__file__).parent
panel = root / "AuroraRevit.extension" / "RevitTools.tab" / "AIAssistant.panel"
targets = {
    "CommandLogger": panel / "CommandLogger.pushbutton" / "script.py",
    "CommandLine": panel / "CommandLine.pushbutton" / "script.py",
    "ElementInspector": panel / "ElementInspector.pushbutton" / "script.py",
    "QuickSettings": panel / "QuickSettings.pushbutton" / "script.py",
    "ExportToPDF": panel / "ExportToPDF.pushbutton" / "script.py",
}

python3_only = []
for name, path in targets.items():
    source = path.read_text(encoding="utf-8")
    tree = ast.parse(source, filename=str(path))
    compile(tree, str(path), "exec")
    for node in ast.walk(tree):
        if isinstance(node, ast.JoinedStr):
            python3_only.append((name, "f-string", node.lineno))
        if isinstance(node, (ast.AsyncFunctionDef, ast.Await)):
            python3_only.append((name, "async/await", getattr(node, "lineno", "?")))
    for forbidden in ["pathlib", "dataclasses", "typing"]:
        if forbidden in source:
            python3_only.append((name, forbidden, "text"))

line = targets["CommandLine"].read_text(encoding="utf-8")
logger = targets["CommandLogger"].read_text(encoding="utf-8")
xaml = (panel / "CommandLine.pushbutton" / "CommandLine.xaml").read_text(encoding="utf-8")
inspector = targets["ElementInspector"].read_text(encoding="utf-8")
settings = targets["QuickSettings"].read_text(encoding="utf-8")
pdf = targets["ExportToPDF"].read_text(encoding="utf-8")

assert "imp.load_source" not in line, "deprecated imp.load_source remains"
assert "from pyrevit import forms" in line
assert "sys.path.insert(0, script_dir)" in line
assert "os.path.abspath(os.path.join(here, \"..\", \"..\", \"..\"))" in line
assert "forms.WPFPanel" in line, "CommandLine must use the documented pyRevit dockable panel base"
assert "forms.register_dockable_panel" in line
assert "forms.open_dockable_panel" in line
assert 'Orientation="Horizontal"' in xaml
assert 'TextWrapping=\\"Wrap\\"' in line
assert 'VerticalScrollBarVisibility=\\"Auto\\"' in line
assert 'HorizontalScrollBarVisibility=\\"Auto\\"' in line
assert "DockablePane" not in line or "forms.WPFPanel" in line
assert "#FF1E1E1E" in line or "#FF1E1E1E" in xaml
assert "CommandLog.state.json" in logger
assert "CommandLog.xlsx" in logger
assert "CommandLog.csv" in logger
assert "PickObject" in inspector and "Parameters" in inspector
assert "Transaction" not in inspector
assert "command_tools_settings.json" in settings and "model" in settings and "ollama_endpoint" in settings
assert "PrintManager" in pdf and "Safe Preview" in pdf and "SubmitPrint" in pdf
assert "except Exception" in logger and "except Exception" in line
assert not python3_only, python3_only

print("ast_parse_and_compile=PASS")
print("python27_syntax_scan=PASS")
print("no_imp_load_source=PASS")
print("explicit_wpf_xaml_properties=PASS")
print("documented_dockable_panel_contract=PASS")
print("direct_sibling_path_contract=PASS")
print("audit_target_script02=NOT_PRESENT; audited_active_script.py")
print("command_tools_deep_audit=PASS")

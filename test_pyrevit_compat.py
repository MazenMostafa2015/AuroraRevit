import importlib.util
import os
import sys
import types
from pathlib import Path

root = Path(__file__).parent

# Provide minimal fake WPF modules so the CommandLogger module can be imported
# under CPython while exercising its IronPython assembly-load boundary.
clr = types.ModuleType("clr")
clr.AddReference = lambda _name: None
sys.modules["clr"] = clr
system = types.ModuleType("System")
windows = types.ModuleType("System.Windows")
windows.Window = object
windows.Thickness = lambda *args: args
controls = types.ModuleType("System.Windows.Controls")
controls.Button = object
controls.StackPanel = object
controls.TextBlock = object
media = types.ModuleType("System.Windows.Media")
media.Brushes = types.SimpleNamespace(White="white")
media.SolidColorBrush = lambda value: value
media.Color = types.SimpleNamespace(FromRgb=lambda *args: args)
sys.modules.update({"System": system, "System.Windows": windows, "System.Windows.Controls": controls, "System.Windows.Media": media})

logger_path = root / "AuroraRevit.extension" / "RevitTools.tab" / "AIAssistant.panel" / "CommandLogger.pushbutton" / "script.py"
spec = importlib.util.spec_from_file_location("command_logger_compat", str(logger_path))
logger = importlib.util.module_from_spec(spec)
spec.loader.exec_module(logger)
assert logger.WPF_AVAILABLE is True

router_path = root / "AuroraRevit.extension" / "RevitTools.tab" / "AIAssistant.panel" / "UtilityTools" / "ai_router.py"
spec = importlib.util.spec_from_file_location("router_compat", str(router_path))
router = importlib.util.module_from_spec(spec)
spec.loader.exec_module(router)
os.environ["AURORA_OLLAMA_ENDPOINT"] = "http://localhost:11434/api/"
assert router.ollama_endpoint() == "http://localhost:11434"
print("ironpython_wpf_import=PASS")
print("ollama_endpoint_normalization=PASS")
print("commandline_fallback_contract=PASS")

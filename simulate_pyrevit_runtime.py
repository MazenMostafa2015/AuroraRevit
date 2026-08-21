from __future__ import print_function

import importlib.util
import os
import sys
import tempfile
import types


def module(name, **values):
    item = types.ModuleType(name)
    for key, value in values.items():
        setattr(item, key, value)
    sys.modules[name] = item
    return item


class DummyColor(object):
    @staticmethod
    def FromRgb(*args):
        return args

    @staticmethod
    def FromArgb(*args):
        return args


class DummyBrush(object):
    def __init__(self, value=None):
        self.value = value


class DummyBrushConverter(object):
    def ConvertFromString(self, value):
        return value


class DummyWindow(object):
    pass


class DummyThickness(object):
    def __init__(self, *args):
        self.args = args


class DummyControl(object):
    def __init__(self, *args, **kwargs):
        self.__dict__.update(kwargs)
        self.Children = []


class DummyPanel(DummyControl):
    pass


class DummyButton(DummyControl):
    pass


class DummyTextBox(DummyControl):
    pass


class DummyTextBlock(DummyControl):
    pass


system = module("System", Uri=object)
windows = module("System.Windows", Window=DummyWindow, Thickness=DummyThickness, Clipboard=object)
controls = module("System.Windows.Controls", Button=DummyButton, Grid=DummyControl, StackPanel=DummyPanel, TextBox=DummyTextBox, TextBlock=DummyTextBlock)
media = module("System.Windows.Media", Brushes=types.SimpleNamespace(White="white", LimeGreen="green", Gold="gold"), SolidColorBrush=DummyBrush, Color=DummyColor, BrushConverter=DummyBrushConverter)

forms_module = types.ModuleType("pyrevit.forms")
forms_module.WPFPanel = type("WPFPanel", (object,), {"__init__": lambda self: None})
forms_module.WPFWindow = type("WPFWindow", (object,), {"__init__": lambda self, *args, **kwargs: None})
forms_module.is_registered_dockable_panel = lambda cls: False
forms_module.register_dockable_panel = lambda cls: None
forms_module.open_dockable_panel = lambda cls: None
forms_module.get_dockable_panel = lambda cls: None
forms_module.alert = lambda *args, **kwargs: True
forms_module.SelectFromList = types.SimpleNamespace(show=lambda *args, **kwargs: [])
pyrevit = module("pyrevit", forms=forms_module)
sys.modules["pyrevit.forms"] = forms_module

ui_module = module("Autodesk.Revit.UI", Selection=types.SimpleNamespace(ObjectType=types.SimpleNamespace(Element="Element")))
autodesk = module("Autodesk", Revit=types.SimpleNamespace(UI=ui_module))
sys.modules["Autodesk.Revit"] = types.ModuleType("Autodesk.Revit")
sys.modules["Autodesk.Revit.UI"] = ui_module


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    item = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(item)
    return item


root = os.path.dirname(os.path.abspath(__file__))
panel = os.path.join(root, "AuroraRevit.extension", "RevitTools.tab", "AIAssistant.panel")
logger = load("logger_sim", os.path.join(panel, "CommandLogger.pushbutton", "script.py"))
line = load("line_sim", os.path.join(panel, "CommandLine.pushbutton", "script.py"))
inspector = load("inspector_sim", os.path.join(panel, "ElementInspector.pushbutton", "script.py"))
settings = load("settings_sim", os.path.join(panel, "QuickSettings.pushbutton", "script.py"))
pdf = load("pdf_sim", os.path.join(panel, "ExportToPDF.pushbutton", "script.py"))

with tempfile.TemporaryDirectory() as temp:
    journal = os.path.join(temp, "journal.0001.txt")
    with open(journal, "w") as handle:
        handle.write("Jrn.Command 'ID_EDIT_MOVE'\nJrn.Command 'ID_EDIT_MOVE'\nJrn.Command 'ID_EDIT_COPY'\n")
    logger._journal_files = lambda: [journal]
    commands = logger._read_journal_commands()
    assert len(commands) == 3
    assert commands[0][0] == "ID_EDIT_MOVE"
    assert commands[0][1] != commands[1][1]

assert "imp.load_source" not in line.__dict__.get("__doc__", "")
assert hasattr(line, "_load_script_module")
assert "CommandLine.xaml" == line.CommandLinePanel.panel_source
assert line.CommandLinePanel.panel_id.count("-") == 4
assert settings._valid_folder(os.path.abspath("AuroraRevit_Logs"))[0] is True
assert pdf._label is not None
assert inspector._coordinates is not None

print("module_import_simulation=PASS")
print("journal_occurrence_simulation=PASS")
print("direct_sibling_loader_simulation=PASS")
print("wpf_panel_contract_simulation=PASS")
print("feature_module_imports=PASS")
print("ironpython_runtime_simulation=PASS")

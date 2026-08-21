# -*- coding: utf-8 -*-
"""Aurora AutoCAD-style command line for pyRevit.

This is an independent pushbutton. It uses pyRevit's documented WPFPanel
registration/opening helpers and never executes generated code automatically.
"""

from __future__ import print_function

import os
import sys
import types

try:
    from System.Net import WebClient
except Exception:
    WebClient = None

try:
    from pyrevit import forms
except Exception:
    forms = None

try:
    import Autodesk.Revit.UI as UI
except Exception:
    UI = None

LOG_DIR = r"C:\AuroraRevit_Logs"
XLSX_PATH = os.path.join(LOG_DIR, "CommandLog.xlsx")
CSV_PATH = os.path.join(LOG_DIR, "CommandLog.csv")
PANEL_ID = "f0d4c9a4-53ab-4dd5-aab4-2d3bb0a1df84"
PANEL_XAML = "CommandLine.xaml"
ACCENT_HEX = "#FF0078D4"
IMPORT_ERRORS = {}


def _load_script_module(script_path, module_name):
    """Load a sibling pyRevit script by absolute path on IronPython 2.7+."""
    script_path = os.path.abspath(script_path)
    script_dir = os.path.dirname(script_path)
    if script_dir not in sys.path:
        sys.path.insert(0, script_dir)
    module = types.ModuleType(module_name)
    module.__file__ = script_path
    try:
        with open(script_path, "rb") as stream:
            source = stream.read()
        code = compile(source, script_path, "exec")
        exec(code, module.__dict__)
        return module
    except Exception as error:
        IMPORT_ERRORS[module_name] = str(error)
        return None


def _load_sibling_script(folder_name, module_name):
    here = os.path.dirname(os.path.abspath(__file__))
    extension_root = os.path.abspath(os.path.join(here, "..", "..", ".."))
    candidates = [
        os.path.join(extension_root, "RevitTools.tab", "AIAssistant.panel", folder_name, "script.py"),
        os.path.join(os.path.dirname(here), folder_name, "script.py"),
    ]
    for script_path in candidates:
        if os.path.isfile(script_path):
            return _load_script_module(script_path, module_name)
    IMPORT_ERRORS[module_name] = "File not found: " + candidates[0]
    return None


def _load_logger():
    return _load_sibling_script("CommandLogger.pushbutton", "aurora_command_logger")


def _load_aichat():
    # Direct filesystem loading avoids dotted pyRevit folder names.
    return _load_sibling_script("AIChat.pushbutton", "aurora_aichat_engine")


def _proxy_health():
    if WebClient is None:
        return None
    for port in [5001, 5000]:
        client = WebClient()
        try:
            client.DownloadString("http://localhost:{0}/health".format(port))
            return port
        except Exception:
            pass
        finally:
            try:
                client.Dispose()
            except Exception:
                pass
    return None


def _invoke_engine(prompt):
    module = _load_aichat()
    if module is None:
        detail = IMPORT_ERRORS.get("aurora_aichat_engine", "unknown import error")
        return {"type": "info", "message": "AIChat could not be loaded: " + detail}
    for name in ["process_command", "handle_command", "send_prompt", "query_ai", "ask_ai"]:
        function = getattr(module, name, None)
        if callable(function):
            try:
                result = function(prompt)
                if isinstance(result, dict):
                    return result
                return {"type": "info", "message": str(result)}
            except Exception as error:
                return {"type": "info", "message": "AIChat engine error: " + str(error)}
    return {"type": "info", "message": "No supported entry point was found in AIChat.pushbutton/script.py."}


def _review_code(code):
    if not forms:
        return
    xaml = """
<Window xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"
        Title=\"Aurora AI Code Review\" Width=\"760\" Height=\"520\"
        WindowStartupLocation=\"CenterOwner\" Background=\"#FF1E1E1E\"
        Foreground=\"White\">
  <Grid Margin=\"16\">
    <Grid.RowDefinitions><RowDefinition Height=\"Auto\"/><RowDefinition Height=\"*\"/><RowDefinition Height=\"Auto\"/></Grid.RowDefinitions>
    <TextBlock Grid.Row=\"0\" Text=\"Safe Preview: review generated code before any execution.\" Margin=\"0,0,0,10\"/>
    <TextBox Grid.Row=\"1\" Name=\"CodeBox\" AcceptsReturn=\"True\" AcceptsTab=\"True\" TextWrapping=\"Wrap\" VerticalScrollBarVisibility=\"Auto\" HorizontalScrollBarVisibility=\"Auto\" IsReadOnly=\"True\" Background=\"#FF161616\" Foreground=\"White\"/>
    <StackPanel Grid.Row=\"2\" Orientation=\"Horizontal\" Margin=\"0,10,0,0\">
      <Button Name=\"CopyButton\" Content=\"Copy Code\" Width=\"110\" Height=\"30\" Background=\"#FF0078D4\" Margin=\"0,0,8,0\"/>
      <Button Name=\"CloseButton\" Content=\"Close\" Width=\"90\" Height=\"30\" Background=\"#FF0078D4\"/>
    </StackPanel>
  </Grid>
</Window>
"""
    review = forms.WPFWindow(xaml, literal_string=True)
    review.CodeBox.Text = str(code)

    def copy_code(_sender, _args):
        try:
            from System.Windows import Clipboard
            Clipboard.SetText(str(code))
        except Exception:
            pass

    review.CopyButton.Click += copy_code
    review.CloseButton.Click += lambda sender, args: review.Close()
    review.show_dialog()


def _show(message, title="Aurora Command Line"):
    if forms:
        forms.alert(message, title=title)


def _last_log_entry():
    logger = _load_logger()
    if logger is None:
        return "CommandLogger could not be loaded."
    try:
        path = logger.XLSX_PATH if os.path.isfile(logger.XLSX_PATH) else logger.CSV_PATH
        if not os.path.isfile(path):
            return "No command log entry is available yet."
        if path.lower().endswith(".xlsx"):
            import openpyxl
            book = openpyxl.load_workbook(path, read_only=True, data_only=True)
            rows = list(book.active.iter_rows(values_only=True))
            book.close()
            if len(rows) > 1:
                return " | ".join([str(value or "") for value in rows[-1]])
        else:
            with open(path, "r") as handle:
                lines = handle.readlines()
            if len(lines) > 1:
                return lines[-1].strip()
    except Exception as error:
        return "Unable to read log: " + str(error)
    return "No command log entry is available yet."


def _log_command(prompt):
    logger = _load_logger()
    if logger is not None:
        try:
            return logger.append_log(prompt, "AI command submitted from Aurora CommandLine")
        except Exception:
            pass
    return None


class CommandLinePanel(forms.WPFPanel if forms else object):
    """Registered Revit dockable panel for the command bar."""

    panel_id = PANEL_ID
    panel_source = PANEL_XAML
    panel_title = "Aurora Command Line"

    def __init__(self):
        if forms:
            forms.WPFPanel.__init__(self)
            self.CommandInput.Text = ""
            self.SendButton.Click += self._send_clicked
            self.ExpandButton.Click += self._expand_clicked
            self.LastLogButton.Click += self._last_log_clicked
            active_port = _proxy_health()
            self.StatusText.Text = "AI/Proxy ready (port {0})".format(active_port) if active_port else "AI/Proxy unavailable"
            self.StatusDot.Foreground = self._brush("#FF66CC66" if active_port else "#FFFF6B6B")

    @staticmethod
    def _brush(value):
        from System.Windows.Media import BrushConverter
        return BrushConverter().ConvertFromString(value)

    def _send_clicked(self, _sender, _args):
        prompt = str(self.CommandInput.Text or "").strip()
        if not prompt:
            _show("Type a Revit command first.")
            return
        self.SendButton.IsEnabled = False
        self.StatusText.Text = "AI/Proxy working..."
        self.StatusDot.Foreground = self._brush("#FFFFC107")
        _log_command(prompt)
        result = _invoke_engine(prompt)
        result_type = str(result.get("type", "info")) if isinstance(result, dict) else "info"
        if result_type == "code":
            _review_code(result.get("content", ""))
        elif result_type == "select":
            _show("Selection request returned:\n\n" + str(result.get("query", "")))
        else:
            _show(str(result.get("message", result)))
        active_port = _proxy_health()
        self.StatusText.Text = "AI/Proxy ready (port {0})".format(active_port) if active_port else "AI/Proxy unavailable"
        self.StatusDot.Foreground = self._brush("#FF66CC66" if active_port else "#FFFF6B6B")
        self.SendButton.IsEnabled = True

    def _expand_clicked(self, _sender, _args):
        module = _load_aichat()
        if module is None:
            _show("AIChat could not be loaded: " + IMPORT_ERRORS.get("aurora_aichat_engine", "unknown error"))
            return
        for name in ["open_chat", "show_chat", "show_window", "main"]:
            function = getattr(module, name, None)
            if callable(function):
                try:
                    function()
                    return
                except TypeError:
                    continue
        _show("No supported full-chat entry point was found in AIChat.pushbutton/script.py.")

    def _last_log_clicked(self, _sender, _args):
        _show(_last_log_entry(), title="Last Command Log Entry")


def _open_panel():
    if not forms:
        return None
    try:
        registered = forms.is_registered_dockable_panel(CommandLinePanel)
    except Exception:
        registered = False
    if not registered:
        try:
            # Registration creates the live WPFPanel provider. Do not call open
            # if this step fails or Revit reports an invalid XAML/resource path.
            forms.register_dockable_panel(CommandLinePanel, default_visible=False)
        except Exception as error:
            detail = "Dockable panel registration failed: " + str(error)
            IMPORT_ERRORS["dockable_panel"] = detail
            _show(detail, title="Aurora Command Line")
            return None
    try:
        forms.open_dockable_panel(CommandLinePanel)
        return True
    except Exception as error:
        detail = "Dockable panel was registered but could not be opened: " + str(error)
        IMPORT_ERRORS["dockable_panel"] = detail
        _show(detail, title="Aurora Command Line")
        return None


if __name__ == "__main__":
    _open_panel()

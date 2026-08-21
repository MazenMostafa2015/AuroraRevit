# -*- coding: utf-8 -*-
"""Aurora AutoCAD-style command line for pyRevit.

Independent pushbutton. Existing AIChat.pushbutton and QuickCommand.pushbutton
are not edited; the AIChat module is loaded directly from its sibling folder.
"""

from __future__ import print_function

import imp
import os
import subprocess
import sys

from System import Uri
from System.Windows import Window, Thickness
from System.Windows.Controls import Button, Grid, StackPanel, TextBox, TextBlock
from System.Windows.Media import Brushes, SolidColorBrush, Color

try:
    from pyrevit import forms
except Exception:
    forms = None

LOG_DIR = r"C:\AuroraRevit_Logs"
XLSX_PATH = os.path.join(LOG_DIR, "CommandLog.xlsx")
CSV_PATH = os.path.join(LOG_DIR, "CommandLog.csv")
PANEL_ID = "Aurora.CommandLine.DockableWindow"
ACCENT = SolidColorBrush(Color.FromRgb(0, 120, 212))


def _load_sibling_script(folder_name, module_name):
    """Load a sibling pyRevit script without relying on package-name syntax."""
    here = os.path.dirname(os.path.abspath(__file__))
    extension_root = os.path.abspath(os.path.join(here, "..", "..", ".."))
    candidates = [
        os.path.join(extension_root, "RevitTools.tab", "AIAssistant.panel", folder_name, "script.py"),
        os.path.join(os.path.dirname(here), folder_name, "script.py"),
    ]
    for script_path in candidates:
        if os.path.isfile(script_path):
            script_dir = os.path.dirname(script_path)
            if script_dir not in sys.path:
                sys.path.insert(0, script_dir)
            try:
                return imp.load_source(module_name, script_path)
            except Exception:
                return None
    return None


def _load_logger():
    return _load_sibling_script("CommandLogger.pushbutton", "aurora_command_logger")


def _load_aichat():
    # This is deliberately a direct path import, not a dotted import of a
    # folder containing a dot in its pyRevit name.
    return _load_sibling_script("AIChat.pushbutton", "aurora_aichat_engine")


def _invoke_engine(prompt):
    module = _load_aichat()
    if module is None:
        return {"type": "info", "message": "AIChat.pushbutton/script.py was not found."}
    candidates = ["process_command", "handle_command", "send_prompt", "query_ai", "ask_ai"]
    for name in candidates:
        function = getattr(module, name, None)
        if callable(function):
            try:
                result = function(prompt)
                if isinstance(result, dict):
                    return result
                return {"type": "info", "message": str(result)}
            except Exception as error:
                return {"type": "info", "message": "AIChat engine error: " + str(error)}
    return {"type": "info", "message": "No supported command entry point was found in AIChat.pushbutton/script.py."}


def _review_code(code):
    review = Window()
    review.Title = "Aurora AI Code Review"
    review.Width = 720
    review.Height = 480
    review.Background = SolidColorBrush(Color.FromRgb(30, 30, 30))
    review.Foreground = Brushes.White
    layout = StackPanel()
    layout.Margin = Thickness(16)
    label = TextBlock(Text="Review generated code before any execution. Nothing is executed by this review window.")
    label.Margin = Thickness(0, 0, 0, 10)
    layout.Children.Add(label)
    editor = TextBox(Text=str(code), AcceptsReturn=True, AcceptsTab=True, TextWrapping=1)
    editor.IsReadOnly = True
    editor.VerticalScrollBarVisibility = 1
    editor.HorizontalScrollBarVisibility = 1
    editor.Background = SolidColorBrush(Color.FromRgb(22, 22, 22))
    editor.Foreground = Brushes.White
    layout.Children.Add(editor)
    buttons = StackPanel()
    buttons.Orientation = 0
    copy_button = Button(Content="Copy Code", Width=110, Height=30, Margin=Thickness(0, 10, 8, 0))
    copy_button.Click += lambda sender, args: _copy_text(str(code))
    buttons.Children.Add(copy_button)
    close_button = Button(Content="Close", Width=90, Height=30, Margin=Thickness(0, 10, 0, 0))
    close_button.Click += lambda sender, args: review.Close()
    buttons.Children.Add(close_button)
    layout.Children.Add(buttons)
    review.Content = layout
    review.ShowDialog()


def _copy_text(text):
    try:
        from System.Windows import Clipboard
        Clipboard.SetText(text)
    except Exception:
        pass


def _show(message, title="Aurora Command Line"):
    if forms:
        forms.alert(message, title=title)


def _last_log_entry():
    logger = _load_logger()
    if logger is not None:
        try:
            path = logger.XLSX_PATH if os.path.isfile(logger.XLSX_PATH) else logger.CSV_PATH
            if os.path.isfile(path):
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


def _dock_or_show(window):
    """Use pyRevit's dockable_window API when present, with a safe fallback."""
    if forms and hasattr(forms, "dockable_window"):
        try:
            return forms.dockable_window(window, dockable_id=PANEL_ID)
        except TypeError:
            try:
                return forms.dockable_window(window)
            except Exception:
                pass
        except Exception:
            pass
    window.Show()
    return window


def show_command_line():
    window = Window()
    window.Title = "Aurora Command Line"
    window.Width = 820
    window.Height = 130
    window.MinHeight = 110
    window.Background = SolidColorBrush(Color.FromRgb(30, 30, 30))
    window.Foreground = Brushes.White
    root = StackPanel()
    root.Margin = Thickness(10)

    status_row = StackPanel()
    status_row.Orientation = 0
    dot = TextBlock(Text="●", Foreground=Brushes.LimeGreen, FontSize=16, Width=24)
    status_row.Children.Add(dot)
    status = TextBlock(Text="AI/Proxy ready", VerticalAlignment=1, Margin=Thickness(0, 0, 12, 0))
    status_row.Children.Add(status)
    expand = Button(Content="Expand Chat", Width=105, Height=26, Margin=Thickness(0, 0, 6, 0))
    expand.Background = ACCENT
    expand.Click += lambda sender, args: _open_full_chat()
    status_row.Children.Add(expand)
    last = Button(Content="Show Last Log Entry", Width=145, Height=26)
    last.Background = ACCENT
    last.Click += lambda sender, args: _show(_last_log_entry(), "Last Command Log Entry")
    status_row.Children.Add(last)
    root.Children.Add(status_row)

    command_row = StackPanel()
    command_row.Orientation = 0
    input_box = TextBox(Height=30, MinWidth=610, Margin=Thickness(0, 10, 8, 0))
    input_box.Background = SolidColorBrush(Color.FromRgb(45, 45, 45))
    input_box.Foreground = Brushes.White
    input_box.Text = "Type a Revit command..."
    command_row.Children.Add(input_box)
    send = Button(Content="Send", Width=80, Height=30, Margin=Thickness(0, 10, 0, 0))
    send.Background = ACCENT
    command_row.Children.Add(send)
    root.Children.Add(command_row)

    def submit(_sender=None, _args=None):
        prompt = str(input_box.Text or "").strip()
        if not prompt or prompt == "Type a Revit command...":
            _show("Type a command first.")
            return
        send.IsEnabled = False
        status.Text = "AI/Proxy working..."
        dot.Foreground = Brushes.Gold
        _log_command(prompt)
        result = _invoke_engine(prompt)
        result_type = str(result.get("type", "info")) if isinstance(result, dict) else "info"
        if result_type == "code":
            _review_code(result.get("content", ""))
        elif result_type == "select":
            _show("Selection request returned:\n\n" + str(result.get("query", "")))
        else:
            _show(str(result.get("message", result)))
        status.Text = "AI/Proxy ready"
        dot.Foreground = Brushes.LimeGreen
        send.IsEnabled = True

    send.Click += submit
    input_box.KeyDown += lambda sender, args: submit() if str(args.Key) == "Return" else None
    window.Content = root
    window.Closed += lambda sender, args: None
    return _dock_or_show(window)


def _open_full_chat():
    module = _load_aichat()
    if module is None:
        _show("AIChat.pushbutton/script.py was not found.")
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


if __name__ == "__main__":
    show_command_line()

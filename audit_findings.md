# AuroraRevit Command Tools Audit Findings

Date: 2026-08-21

The repository currently contains `CommandLine.pushbutton/script.py`; the user-referenced `script02.py` is not present, so the audit target is the active `script.py` and a compatibility alias will be added.

The current command bar imports `imp.load_source`. This is available to IronPython 2.7 but is deprecated/removed in newer CPython versions and the current code suppresses all loader exceptions, creating a hidden ImportError trap. The loader will be replaced with a small cross-engine module loader that executes a sibling file by explicit path and reports the failure.

The current code calls `forms.dockable_window`, but the official pyRevit forms reference exposes `WPFPanel`, `register_dockable_panel`, `open_dockable_panel`, and `get_dockable_panel`; no `dockable_window` function is documented. The current code therefore silently falls back to a normal WPF window instead of a Revit dockable pane. It will be changed to a real `forms.WPFPanel` implementation with registration/opening and a safe fallback.

The current WPF code uses integer values for enum properties (`Orientation = 0`, `TextWrapping = 1`, and scroll-bar visibility = 1). These may be coerced by WPF but are brittle in IronPython; they will be replaced with explicit WPF enum members. Unused `System.Uri`, `Grid`, and alignment imports will be removed.

The source contains no f-strings, pathlib, dataclasses, or other obvious Python 3-only syntax. `except Exception as error` is valid in Python 2.7. The new code will continue to avoid Python 3-only syntax and will be parsed with Python 3 AST/compile checks plus a simulated IronPython constraint scanner.

Reference checked: https://docs.pyrevitlabs.io/reference/pyrevit/forms/ (official pyRevit forms reference, including WPFPanel and dockable-panel helpers).

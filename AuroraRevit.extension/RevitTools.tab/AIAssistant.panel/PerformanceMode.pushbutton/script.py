# -*- coding: utf-8 -*-
"""Aurora Background Calculation Killer / Performance Mode button."""
from __future__ import print_function
import os
import types
try:
    from pyrevit import forms
except Exception:
    forms = None


def _core():
    panel = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    path = os.path.join(panel, "UtilityTools", "utility_core.py")
    if not os.path.isfile(path):
        return None
    module = types.ModuleType("aurora_utility_core_performance")
    module.__file__ = path
    try:
        with open(path, "rb") as stream:
            source = stream.read()
        exec(compile(source, path, "exec"), module.__dict__)
        return module
    except Exception as error:
        if forms:
            forms.alert("Utility core could not be loaded:\n\n" + str(error), title="Aurora Performance Mode")
        return None


def main():
    core = _core()
    if core:
        try:
            core.background_calculation_killer()
        except Exception as error:
            if forms:
                forms.alert("Performance mode failed safely:\n\n" + str(error), title="Aurora Performance Mode")


if __name__ == "__main__":
    main()


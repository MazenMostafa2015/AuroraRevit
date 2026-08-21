# -*- coding: utf-8 -*-
"""Aurora Smart Safety Detailer button."""
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
    module = types.ModuleType("aurora_utility_core_safety")
    module.__file__ = path
    try:
        with open(path, "rb") as stream:
            source = stream.read()
        exec(compile(source, path, "exec"), module.__dict__)
        return module
    except Exception as error:
        if forms:
            forms.alert("Utility core could not be loaded:\n\n" + str(error), title="Aurora Smart Safety Detailer")
        return None


def main():
    core = _core()
    if core:
        try:
            core.smart_safety_detailer()
        except Exception as error:
            text = str(error).lower()
            if "cancel" not in text and "abort" not in text and "pick operation" not in text:
                if forms:
                    forms.alert("Safety detailer failed safely:\n\n" + str(error), title="Aurora Smart Safety Detailer")


if __name__ == "__main__":
    main()


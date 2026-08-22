# -*- coding: utf-8 -*-
"""Aurora pyRevit chat compatibility entry point.

The C# dockable pane is the primary chat UI. This button preserves the
traditional AIChat.pushbutton path and routes prompts through ai_router.py.
"""
from __future__ import print_function

import os
import types

try:
    from pyrevit import forms
except Exception:
    forms = None


def _router():
    panel = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    path = os.path.join(panel, "UtilityTools", "ai_router.py")
    if not os.path.isfile(path):
        return None
    module = types.ModuleType("aurora_ai_router_compat")
    module.__file__ = path
    try:
        with open(path, "rb") as stream:
            source = stream.read()
        exec(compile(source, path, "exec"), module.__dict__)
        return module
    except Exception:
        return None


def process_command(prompt):
    router = _router()
    if router is None:
        return {"type": "info", "message": "Aurora AI router is not installed."}
    return router.smart_query(prompt)


def handle_command(prompt):
    return process_command(prompt)


def send_prompt(prompt):
    return process_command(prompt)


def main():
    if not forms:
        return
    prompt = forms.ask_for_string(
        default="",
        prompt="Ask Aurora using the selected OpenAI/Ollama provider.",
        title="Aurora AI Chat")
    if not prompt or not prompt.strip():
        return
    result = process_command(prompt.strip())
    if isinstance(result, dict):
        message = result.get("message") or result.get("content") or result.get("response") or str(result)
    else:
        message = str(result)
    forms.alert(message, title="Aurora AI Chat")


if __name__ == "__main__":
    main()

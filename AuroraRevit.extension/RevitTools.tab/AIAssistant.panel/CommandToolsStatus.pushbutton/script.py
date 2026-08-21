# -*- coding: utf-8 -*-
"""Aurora Command Tools diagnostics companion button."""
from __future__ import print_function

import os
import socket

try:
    from pyrevit import forms
except Exception:
    forms = None

LOG_DIR = r"C:\AuroraRevit_Logs"
PROXY_HOST = "127.0.0.1"
PROXY_PORTS = [5000, 5001]


def _sibling_exists(folder):
    here = os.path.dirname(os.path.abspath(__file__))
    panel = os.path.dirname(here)
    return os.path.isfile(os.path.join(panel, folder, "script.py"))


def _port_status(port):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(0.25)
    try:
        return sock.connect_ex((PROXY_HOST, port)) == 0
    except Exception:
        return False
    finally:
        sock.close()


def main():
    try:
        version = str(__revit__.Application.VersionNumber)
    except Exception:
        version = "Unknown"
    journal_root = os.environ.get("LOCALAPPDATA", "")
    journal_root = os.path.join(journal_root, "Autodesk", "Revit", "Autodesk Revit " + version, "Journals")
    ports = [str(port) + (" listening" if _port_status(port) else " unavailable") for port in PROXY_PORTS]
    lines = [
        "Aurora Command Tools Status",
        "",
        "Revit version: " + version,
        "CommandLogger: " + ("installed" if _sibling_exists("CommandLogger.pushbutton") else "missing"),
        "CommandLine: " + ("installed" if _sibling_exists("CommandLine.pushbutton") else "missing"),
        "CommandLogViewer: " + ("installed" if _sibling_exists("CommandLogViewer.pushbutton") else "missing"),
        "Log folder: " + ("ready" if os.path.isdir(LOG_DIR) else "will be created on first use"),
        "Journal folder: " + ("found" if os.path.isdir(journal_root) else "not found"),
        "Proxy ports: " + ", ".join(ports),
    ]
    if forms:
        forms.alert("\n".join(lines), title="Aurora Command Tools Status")


if __name__ == "__main__":
    main()

# -*- coding: utf-8 -*-
"""Aurora Command Log Viewer companion button."""
from __future__ import print_function

import csv
import os
import subprocess

try:
    from pyrevit import forms
except Exception:
    forms = None

LOG_DIR = r"C:\AuroraRevit_Logs"
XLSX_PATH = os.path.join(LOG_DIR, "CommandLog.xlsx")
CSV_PATH = os.path.join(LOG_DIR, "CommandLog.csv")


def _rows():
    path = XLSX_PATH if os.path.isfile(XLSX_PATH) else CSV_PATH
    if not os.path.isfile(path):
        return path, []
    try:
        if path.lower().endswith(".xlsx"):
            import openpyxl
            book = openpyxl.load_workbook(path, read_only=True, data_only=True)
            rows = list(book.active.iter_rows(values_only=True))
            book.close()
            return path, rows[1:]
        with open(path, "r") as handle:
            return path, list(csv.reader(handle))[1:]
    except Exception as error:
        return path, [["ERROR", str(error)]]


def _open_folder():
    if not os.path.isdir(LOG_DIR):
        os.makedirs(LOG_DIR)
    try:
        os.startfile(LOG_DIR)
    except Exception:
        subprocess.Popen(["explorer.exe", LOG_DIR])


def main():
    path, rows = _rows()
    recent = rows[-12:]
    lines = ["Aurora Command Log", "", "File: " + path, "Entries: " + str(len(rows)), ""]
    for row in recent:
        lines.append(" | ".join([str(value or "") for value in row]))
    if not recent:
        lines.append("No entries yet. Run CommandLogger or CommandLine first.")
    if forms:
        forms.alert("\n".join(lines), title="Aurora Command Log Viewer")
        _open_folder()
    else:
        _open_folder()


if __name__ == "__main__":
    main()

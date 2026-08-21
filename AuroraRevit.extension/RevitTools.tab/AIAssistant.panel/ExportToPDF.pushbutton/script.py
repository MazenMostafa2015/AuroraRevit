# -*- coding: utf-8 -*-
"""Aurora Export to PDF with safe preview and Revit PrintManager."""
from __future__ import print_function

import os

try:
    from pyrevit import forms, revit, DB
except Exception:
    forms = None
    revit = None
    DB = None


def _label(view):
    try:
        number = view.SheetNumber
        title = view.Name
        return "Sheet {0} - {1}".format(number, title)
    except Exception:
        return "{0} - {1}".format(str(view.ViewType), str(view.Name))


def _printable_views():
    result = []
    for view in DB.FilteredElementCollector(revit.doc).OfClass(DB.View).ToElements():
        try:
            if view.IsTemplate:
                continue
        except Exception:
            pass
        try:
            if not view.CanBePrinted:
                continue
        except Exception:
            pass
        result.append(view)
    result.sort(key=lambda item: _label(item).lower())
    return result


def _confirm(selection, folder):
    preview = ["Safe Preview — nothing has been printed yet.", "", "Target folder: " + folder, "Selected items: " + str(len(selection)), ""]
    preview.extend(["  " + _label(view) for view in selection[:40]])
    if len(selection) > 40:
        preview.append("  ... and {0} more".format(len(selection) - 40))
    preview.append("")
    preview.append("The configured Revit PDF printer will be used. Continue?")
    return forms.alert("\n".join(preview), title="Aurora Export to PDF", yes=True, no=True)


def _apply_print_set(print_manager, views):
    view_set = DB.ViewSet()
    for view in views:
        view_set.Insert(view)
    setting = print_manager.ViewSheetSetting
    transaction = DB.Transaction(revit.doc, "Aurora PDF Print Set")
    transaction.Start()
    try:
        try:
            setting.InSession.Views = view_set
        except Exception:
            current = setting.CurrentViewSheetSet
            current.Views = view_set
        transaction.Commit()
    except Exception:
        try:
            transaction.RollBack()
        except Exception:
            pass
        raise


def main():
    if not forms or not revit or not DB:
        return
    views = _printable_views()
    if not views:
        forms.alert("No printable sheets or views were found in the active document.", title="Aurora Export to PDF")
        return
    labels = [_label(view) for view in views]
    selected_labels = forms.SelectFromList.show(
        labels,
        title="Select sheets/views for PDF",
        button_name="Preview PDF Export",
        multiselect=True,
    )
    if not selected_labels:
        return
    selected = [views[labels.index(label)] for label in selected_labels]
    folder = forms.pick_folder(title="Choose a folder for the PDF output")
    if not folder:
        return
    if not _confirm(selected, folder):
        return
    try:
        print_manager = revit.doc.PrintManager
        print_manager.PrintRange = DB.PrintRange.Select
        try:
            print_manager.SelectNewPrintDriver("Microsoft Print to PDF")
        except Exception:
            pass
        try:
            print_manager.PrintToFile = True
            print_manager.PrintToFileName = os.path.join(folder, "AuroraRevit_Export.pdf")
        except Exception:
            pass
        _apply_print_set(print_manager, selected)
        print_manager.Apply()
        print_manager.SubmitPrint()
        forms.alert("PDF export submitted to the configured Revit printer.", title="Aurora Export to PDF")
    except Exception as error:
        forms.alert(
            "PDF export was not submitted. Check the configured PDF printer and try again.\n\n" + str(error),
            title="Aurora Export to PDF",
        )


if __name__ == "__main__":
    main()

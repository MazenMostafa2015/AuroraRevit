# -*- coding: utf-8 -*-
"""Aurora Element Inspector: read-only element inspection with safe preview."""
from __future__ import print_function

import os

try:
    from pyrevit import forms, revit, DB, UI
except Exception:
    forms = None
    revit = None
    DB = None
    UI = None


def _text(value):
    if value is None:
        return ""
    try:
        return str(value)
    except Exception:
        return repr(value)


def _parameter_value(parameter):
    try:
        value = parameter.AsValueString()
        if value is not None:
            return _text(value)
    except Exception:
        pass
    try:
        storage = parameter.StorageType
        if storage == DB.StorageType.String:
            return _text(parameter.AsString())
        if storage == DB.StorageType.Integer:
            return _text(parameter.AsInteger())
        if storage == DB.StorageType.Double:
            return _text(parameter.AsDouble())
        if storage == DB.StorageType.ElementId:
            return "ElementId(" + _text(parameter.AsElementId().IntegerValue) + ")"
    except Exception:
        pass
    return "<unavailable>"


def _xyz_text(point):
    if point is None:
        return ""
    return "({0:.6f}, {1:.6f}, {2:.6f}) ft".format(point.X, point.Y, point.Z)


def _coordinates(element):
    values = []
    try:
        location = element.Location
        if isinstance(location, DB.LocationPoint):
            values.append("LocationPoint: " + _xyz_text(location.Point))
        elif isinstance(location, DB.LocationCurve):
            curve = location.Curve
            values.append("Curve start: " + _xyz_text(curve.GetEndPoint(0)))
            values.append("Curve end: " + _xyz_text(curve.GetEndPoint(1)))
    except Exception:
        pass
    try:
        box = element.get_BoundingBox(None)
        if box:
            values.append("Bounding box min: " + _xyz_text(box.Min))
            values.append("Bounding box max: " + _xyz_text(box.Max))
    except Exception:
        pass
    return values or ["No location or bounding-box coordinates available."]


def _report(element):
    doc = revit.doc
    lines = ["Aurora Element Inspector — Safe Preview", "", "ElementId: " + _text(element.Id.IntegerValue)]
    lines.append("Class: " + _text(element.GetType().FullName))
    try:
        lines.append("Category: " + _text(element.Category.Name if element.Category else "None"))
    except Exception:
        lines.append("Category: <unavailable>")
    try:
        type_id = element.GetTypeId()
        lines.append("TypeId: " + _text(type_id.IntegerValue))
        element_type = doc.GetElement(type_id)
        if element_type:
            lines.append("Type name: " + _text(element_type.Name))
            lines.append("Type class: " + _text(element_type.GetType().FullName))
            try:
                if element_type.FamilyName:
                    lines.append("Family: " + _text(element_type.FamilyName))
            except Exception:
                pass
    except Exception:
        lines.append("Type: <unavailable>")
    lines.extend(["", "Coordinates"])
    lines.extend(["  " + item for item in _coordinates(element)])
    lines.extend(["", "Parameters"])
    parameters = []
    try:
        for parameter in element.Parameters:
            try:
                name = parameter.Definition.Name
            except Exception:
                name = "<unnamed>"
            parameters.append((name, _parameter_value(parameter)))
    except Exception:
        pass
    parameters.sort(key=lambda item: item[0].lower())
    for name, value in parameters:
        lines.append("  {0}: {1}".format(name, value))
    if not parameters:
        lines.append("  No parameters available.")
    return "\n".join(lines)


def _show_report(report):
    if not forms:
        return
    xaml = """
<Window xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Title=\"Aurora Element Inspector\" Width=\"760\" Height=\"620\" Background=\"#FF1E1E1E\" Foreground=\"White\">
  <Grid Margin=\"14\">
    <Grid.RowDefinitions><RowDefinition Height=\"*\"/><RowDefinition Height=\"Auto\"/></Grid.RowDefinitions>
    <TextBox Name=\"ReportBox\" Grid.Row=\"0\" TextWrapping=\"Wrap\" AcceptsReturn=\"True\" IsReadOnly=\"True\" VerticalScrollBarVisibility=\"Auto\" HorizontalScrollBarVisibility=\"Auto\" Background=\"#FF161616\" Foreground=\"White\" />
    <StackPanel Grid.Row=\"1\" Orientation=\"Horizontal\" Margin=\"0,10,0,0\">
      <Button Name=\"CopyButton\" Content=\"Copy Report\" Width=\"110\" Height=\"30\" Background=\"#FF0078D4\" Foreground=\"White\" Margin=\"0,0,8,0\" />
      <Button Name=\"CloseButton\" Content=\"Close\" Width=\"90\" Height=\"30\" Background=\"#FF0078D4\" Foreground=\"White\" />
    </StackPanel>
  </Grid>
</Window>
"""
    window = forms.WPFWindow(xaml, literal_string=True)
    window.ReportBox.Text = report

    def copy_report(_sender, _args):
        try:
            from System.Windows import Clipboard
            Clipboard.SetText(report)
        except Exception:
            pass

    window.CopyButton.Click += copy_report
    window.CloseButton.Click += lambda sender, args: window.Close()
    window.show_dialog()


def main():
    if not forms or not revit or not DB or not UI:
        return
    try:
        reference = revit.uidoc.Selection.PickObject(UI.Selection.ObjectType.Element, "Pick a Revit element to inspect")
        element = revit.doc.GetElement(reference.ElementId)
        if element:
            _show_report(_report(element))
    except Exception as error:
        if "cancel" not in _text(error).lower():
            forms.alert("Element inspection failed:\n\n" + _text(error), title="Aurora Element Inspector")


if __name__ == "__main__":
    main()

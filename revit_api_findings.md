# Revit API Findings

The Autodesk Developer Blog confirms that `Document.PrintManager` is the entry point, `PrintManager.ViewSheetSetting` controls the print set, `PrintRange.Select` must be set before accessing the selected-view set, and `ViewSheetSetting.InSession.Views` should be assigned inside a Revit transaction. Printable views are filtered with `View.CanBePrinted`. The ExportToPDF button follows this sequence and adds Safe Preview plus explicit confirmation before calling `Apply()` and `SubmitPrint()`.

Sources:

- https://blog.autodesk.io/set-views-to-print-with-revitapi/
- https://www.revitapidocs.com/2025/29599e18-cad8-813e-dc6e-04350fe37944.htm

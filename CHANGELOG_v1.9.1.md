# AuroraRevit v1.9.1 — Full Utility Integration

## Release note

The requested `v1.9.0` tag already exists as the published Command Tools Edition, so this full-utility integration is released as the next non-conflicting version, `v1.9.1`, with the requested title theme: **AuroraRevit v1.9.1 — Full Utility Integration**.

## What was added

`UtilityTools.pushbutton/script.py` is a single IronPython 2.7-compatible utility entry point under `RevitTools.tab/AIAssistant.panel`. It provides the six requested commands:

| Command | Implementation |
| --- | --- |
| Export Current View to PDF | Selects output folder and filename, prompts for Portrait/Landscape, A3/A4/Legal, and Fit/100% zoom, displays Safe Preview, then uses Revit `PrintManager`, `PrintRange.Select`, `ViewSheetSetting.InSession.Views`, and `SubmitPrint`. |
| Export Active Schedule to Excel | Requires an active `ViewSchedule`, reads schedule fields and body cells, exports a formatted workbook with dark-blue/white headers, and falls back to CSV if `openpyxl` is unavailable. |
| Batch Parameter Translator | Selects category, source parameter, target parameter, search text, and replacement text; previews the match count; updates writable parameters inside a `DB.Transaction`; and records the operation in the existing command log. |
| Background Calculation Killer | Uses an explicitly limited and reversible active-view performance mode (coarse detail and wireframe where supported) rather than falsely claiming a global API switch that Revit does not expose. |
| Restore Calculations | Restores the saved active-view display settings from `C:\AuroraRevit_Logs\\AuroraCalculationState.json`. |
| Smart Safety Detailer | Picks a floor/slab, calculates a planar boundary preview, accepts a default 1.2-metre spacing, confirms explicitly, and creates a railing path using the first loaded `RailingType`. The final baluster spacing remains controlled by the loaded Revit type. |
| Schedule Export to Excel | Provides the same schedule extraction with minimal formatting for fast data handoff. |

The previously added `ElementInspector`, `QuickSettings`, and `ExportToPDF` companion buttons remain included, so the panel now contains the requested utility bundle plus the three high-value companion buttons without exceeding the requested expansion limit.

## Safety design

No arbitrary AI-generated code is executed. Model-changing commands use Safe Preview and explicit confirmation. Element inspection is read-only. Parameter translation checks target writability and rolls back on failure. The performance command saves prior settings and provides a restore path. PDF export previews the exact active view, destination, printer settings, and output filename before submission. Railing creation is guarded by a floor selection, boundary detection, type availability, preview, and transaction rollback.

## Testing and fixes

The audit suite was rerun with AST parsing and Python compilation. All eight pyRevit scripts compile successfully. The IronPython scan found no f-strings, async/await, pathlib, dataclasses, or other Python 3-only constructs. The simulated runtime imported the logger, command bar, existing companion modules, and UtilityTools with fake .NET/pyRevit/Revit/WPF objects. It exercised repeated journal-command occurrence tracking, direct sibling loading, WPF panel discovery, schedule export helper availability, and safe dialog entry points.

The installer and CI validators passed for all eight scripts and the CommandLine XAML resource. The workflow stages `UtilityTools.pushbutton` in `pyRevit_Extensions`, verifies it before Inno Setup, includes it in the installer payload, and publishes the final Setup.exe only after successful Revit 2023, 2024, and 2025 payload builds.

## Installer and release

The installer is version `1.9.1`. It installs `UtilityTools.pushbutton` into `%APPDATA%\\pyRevit\\Extensions\\AuroraRevit.extension\\RevitTools.tab\\AIAssistant.panel`, creates the existing **Aurora Command Tools** shortcut, and adds **Aurora Utility Tools**, both opening `C:\AuroraRevit_Logs`.

## References

[1]: https://docs.pyrevitlabs.io/reference/pyrevit/forms/ "Official pyRevit forms reference"
[2]: https://blog.autodesk.io/set-views-to-print-with-revitapi/ "Autodesk Developer Blog: Set views to print with RevitAPI"
[3]: https://www.revitapidocs.com/2025/29599e18-cad8-813e-dc6e-04350fe37944.htm "Revit API PrintManager reference"

The WPF and pyRevit panel integration follows the official forms reference [1]. The print-selection sequence follows Autodesk’s documented Revit API flow [2] and the PrintManager API reference [3].

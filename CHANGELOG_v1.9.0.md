# AuroraRevit v1.9.0 — Command Tools Edition

## Release summary

AuroraRevit v1.9.0 expands the pyRevit command-tools bundle from four to seven independent pushbuttons and hardens the released command bar for strict IronPython 2.7 execution.

## Deep audit and fixes

| Area | Result |
| --- | --- |
| AST and compile checks | All seven button scripts and the CommandLine XAML-backed module parse and compile successfully. |
| Python 2.7 compatibility | No f-strings, `async`/`await`, `pathlib`, `dataclasses`, or other Python 3-only constructs remain in the pyRevit scripts. |
| Hidden imports | Removed unused `System.Uri`, alignment, and grid imports from the logger. Replaced the deprecated `imp.load_source` path with an explicit `types.ModuleType` + `compile` + `exec` loader that reports import failures. |
| Dockable WPF behavior | Replaced the undocumented `forms.dockable_window()` attempt with the documented `forms.WPFPanel`, `register_dockable_panel`, `open_dockable_panel`, and `get_dockable_panel` flow. Added the required `CommandLine.xaml` resource. |
| WPF properties | Replaced brittle integer enum values with explicit XAML values such as `Orientation="Horizontal"`, `TextWrapping="Wrap"`, and automatic scroll bars. |
| Command logging | Journal occurrences are tracked by journal path and character offset, so repeated commands are not collapsed into a single row on refresh. |
| Runtime simulation | A fake .NET/pyRevit/Revit harness successfully imported all feature modules, exercised journal occurrence parsing, validated the direct sibling loader, and checked the dockable-panel contract. |

The referenced `CommandLine.pushbutton/script02.py` was not present in the repository. The active released file was `CommandLine.pushbutton/script.py`, so that file was audited and corrected instead of adding a second executable script that could create duplicate pyRevit behavior.

## New pushbuttons

| Pushbutton | Behavior and safety boundary |
| --- | --- |
| `ElementInspector.pushbutton` | Uses `Selection.PickObject` to inspect one element. It reports IDs, category, type/family, location or bounding-box coordinates, and parameters in a read-only report window. It never starts a transaction. |
| `QuickSettings.pushbutton` | Saves model, Ollama endpoint, log folder, and theme preferences to a per-user JSON file under `%APPDATA%\\AuroraRevit`. It validates that custom log paths are absolute and does not modify the Revit model. |
| `ExportToPDF.pushbutton` | Filters printable sheets/views, lets the user choose a range, displays a Safe Preview, asks for confirmation, configures `PrintManager`, assigns `ViewSheetSetting.InSession.Views` inside a transaction, and submits the print. It does not silently print. |

These features were chosen to complete the command-tool workflow: inspect before asking the AI, configure the local experience without editing the model, and produce a controlled PDF deliverable from selected views. The PDF implementation follows Autodesk’s documented `PrintManager`, `PrintRange.Select`, `ViewSheetSetting.InSession.Views`, `View.CanBePrinted`, and transaction sequence.

## Installer and CI

`installer/installer.iss` is version `1.9.0` and installs all seven pyRevit buttons plus `CommandLine.xaml` to `%APPDATA%\\pyRevit\\Extensions\\AuroraRevit.extension\\RevitTools.tab\\AIAssistant.panel`. It creates `C:\AuroraRevit_Logs` and the **Aurora Command Tools** desktop shortcut.

`.github/workflows/build-revit-addin.yml` uses version `1.9.0`, stages the source tree in `pyRevit_Extensions`, verifies every required script and the CommandLine XAML resource, includes them in the Inno Setup payload, and publishes the `AuroraRevit-Setup.exe` asset after successful Revit 2023/2024/2025 builds.

## References

[1]: https://docs.pyrevitlabs.io/reference/pyrevit/forms/ "Official pyRevit forms reference"
[2]: https://blog.autodesk.io/set-views-to-print-with-revitapi/ "Autodesk Developer Blog: Set views to print with RevitAPI"
[3]: https://www.revitapidocs.com/2025/29599e18-cad8-813e-dc6e-04350fe37944.htm "Revit API PrintManager reference"

The dockable-panel corrections follow the official pyRevit forms reference [1]. The PDF export sequence follows Autodesk’s Revit API example and PrintManager reference [2] [3].

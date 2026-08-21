# AuroraRevit v1.9.2 — Revit-Tested Utility UX

## Release summary

AuroraRevit v1.9.2 addresses the issues reported from a real Revit 2025 test session and improves the pyRevit ribbon experience. The release fixes the dockable CommandLine startup failure, removes the misleading legacy-example preview message, treats normal element-pick cancellation correctly, adds icons and descriptions to every visible Aurora button, and replaces the single UtilityTools menu with six independent pushbuttons.

## Revit-tested fixes

### Dockable CommandLine

The previous command-line button could continue to `open_dockable_panel` after registration had failed, producing the message that the requested dockable pane had not been created yet. The fix follows the documented pyRevit WPFPanel lifecycle: it checks registration, registers the panel with `default_visible=False` when needed, stops immediately and reports the registration error if creation fails, and opens the panel only after successful registration. The panel continues to use the documented relative `CommandLine.xaml` resource and stable GUID.

### Example Gallery

Several embedded examples intentionally contain prompts without executable `codeTemplate` values. The previous UI displayed `No code template is available for this legacy example.` The selection handler now generates a safe, read-only C# preview scaffold containing the prompt and an explicit note that no model-changing code is executed from the preview. The visible labels now describe the area as `40 built-in templates` and `Safe C# Preview` rather than implying every prompt has a runnable template.

### Element Inspector

Revit and pyRevit can report Escape as `The user aborted the pick operation.` The inspector now recognizes cancellation, abort, and pick-operation messages as normal exits and does not show a failure dialog when the user simply presses Escape.

### Command Tools Status

The status button now reports the installed state of all current command and utility buttons, the shared UtilityTools core, the retired bundled UtilityTools menu, log/journal folders, and proxy ports 5000/5001.

## Separate utility buttons

The old `UtilityTools.pushbutton` menu has been removed from the active extension tree. Its shared implementation is now stored in the non-button `UtilityTools/utility_core.py` module. The six commands are independently visible and launchable:

| Button | Safety boundary |
| --- | --- |
| `ExportCurrentViewPDF.pushbutton` | Selects print settings, shows a Safe Preview, and confirms before native Revit printing. |
| `ExportScheduleExcel.pushbutton` | Exports the active schedule with Excel/CSV fallback and does not modify the model. |
| `BatchParameterTranslator.pushbutton` | Previews matches and updates only writable parameters inside an explicit transaction after confirmation. |
| `PerformanceMode.pushbutton` | Applies a reversible active-view display mode and records the prior state. |
| `RestorePerformanceMode.pushbutton` | Restores the saved active-view display state. |
| `SmartSafetyDetailer.pushbutton` | Previews floor-boundary length and confirms railing creation; pick cancellation is safe. |

## Icons and descriptions

All 13 visible Aurora AIAssistant pushbuttons now ship with a `bundle.yaml` title/tooltip description and a lightweight `icon.png`. The installer recursively includes these assets, and the Windows CI staging gate verifies every icon and description before Inno Setup compilation.

## Installer and CI

The installer is version `1.9.2`. It installs the shared `UtilityTools` core and six separate utility pushbutton folders into the user pyRevit extension directory. An Inno Setup install-delete rule removes the old `UtilityTools.pushbutton` folder during upgrade so the obsolete menu does not remain visible after installation.

The workflow uses release version `1.9.2`, stages the full extension tree, validates scripts, XAML, icons, and bundle descriptions, compiles the C# add-in and local proxies for Revit 2023, 2024, and 2025, compiles `AuroraRevit-Setup.exe`, and publishes the tagged release asset.

## Validation evidence

The local validation suite passed AST parsing, Python compilation, XAML XML parsing, installer path contracts, workflow staging contracts, README release contracts, direct sibling-loading simulation, journal occurrence simulation, WPF panel contract simulation, and imports for the shared core plus all six utility wrappers. A live Revit GUI smoke test was not available in the Linux build environment; the reported Revit screenshots were used as the defect specification and the corrected release must still be verified once installed in the user’s Revit environment.

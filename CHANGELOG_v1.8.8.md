# AuroraRevit v1.8.8 — Command Tools Edition

## Release highlights

AuroraRevit v1.8.8 adds an installer-integrated pyRevit command-tools bundle and publishes the new `AuroraRevit-Setup.exe` through GitHub Releases.

## Added pyRevit pushbuttons

| Pushbutton | Description |
| --- | --- |
| `CommandLogger.pushbutton` | Scans recent Revit journals, records command occurrences, translates common native command IDs, and writes `C:\AuroraRevit_Logs\CommandLog.xlsx` with CSV fallback. |
| `CommandLine.pushbutton` | Adds a dark AutoCAD-style command bar with AI/proxy status, Send, Expand Chat, Show Last Log Entry, direct AIChat sibling loading, and generated-code review. |
| `CommandLogViewer.pushbutton` | Displays recent command-log rows and opens the log folder. |
| `CommandToolsStatus.pushbutton` | Reports button installation, Revit journal availability, log-folder readiness, and proxy port status. |

## Installer and CI

The Inno Setup package now copies the pyRevit extension tree into `%APPDATA%\\pyRevit\\Extensions\\AuroraRevit.extension\\RevitTools.tab\\AIAssistant.panel`. It creates `C:\AuroraRevit_Logs` and a desktop shortcut named **Aurora Command Tools**.

The GitHub Actions workflow stages `pyRevit_Extensions`, validates all four `script.py` files, includes them in the Inno Setup payload, compiles the single-file installer, and publishes the release asset.

## Validation

The local source validator passed for four pyRevit buttons, version `1.8.8`, installer paths, workflow staging, and README documentation. The tagged Windows workflow completed successfully, including all Revit 2023/2024/2025 payload builds, Inno Setup compilation, installer upload, and GitHub Release publication.

The installer payload was verified by the CI staging gate immediately before Inno Setup compilation. Direct binary extraction was not used because the current Inno Setup loader revision is not supported by the available Linux extraction utilities.

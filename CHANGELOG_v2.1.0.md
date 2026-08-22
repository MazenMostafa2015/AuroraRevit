# AuroraRevit v2.1.0 — Unified Hybrid AI Installer

## Release summary

AuroraRevit v2.1.0 packages the complete Revit 2023–2025 add-in, self-contained .NET 8 AI proxy services, the full pyRevit command extension, and a safer Windows workstation deployment workflow into one release pipeline.

## Installer

The Inno Setup package is versioned as `2.1.0` and produces `AuroraRevit-Setup.exe`. It detects supported Revit installations, installs the matching `AuroraRevit.RevitAddin.dll` and `.addin` manifest into the per-user Revit Addins directory for each detected version, and avoids installing a manifest for an unsupported or absent Revit version.

The installer checks for .NET 8, offers a `winget` installation attempt for the .NET 8 Desktop Runtime when available, and opens the official Microsoft download page as a fallback. It records the absence of the Python launcher without blocking the normal runtime installation because the compiled add-in and self-contained proxies do not require Python to run. Ollama detection remains non-fatal and offers the official Windows download page when Ollama is absent.

The package creates **AuroraRevit AI (Cloud)**, **AuroraRevit AI (Local)**, **Aurora Command Tools**, and **Aurora Utility Tools** shortcuts. It also removes the obsolete `UtilityTools.pushbutton` directory to prevent duplicate ribbon entries.

## Included payload

The release contains the following payload groups:

- `AuroraRevit.RevitAddin.dll` and matching manifests for Revit 2023, 2024, and 2025.
- Self-contained `win-x64` `AiProxy.exe` and `AuroraRevit.ProxyGui.exe`.
- The complete `AuroraRevit.extension` pyRevit tree.
- `AIChat.pushbutton`, `CommandLogger.pushbutton`, `CommandLine.pushbutton`, `CommandLogViewer.pushbutton`, `CommandToolsStatus.pushbutton`, `ElementInspector.pushbutton`, `QuickSettings.pushbutton`, `ExportToPDF.pushbutton`, and the separate utility buttons.
- Shared `UtilityTools\ai_router.py`, `utility_core.py`, button metadata, icons, and CommandLine XAML resources.
- `OllamaLauncher.ps1` and provider-specific launcher shortcuts.

## Runtime fixes

The IronPython CommandLogger now loads `PresentationFramework`, `PresentationCore`, and `WindowsBase` explicitly before importing WPF namespaces, preventing `ImportError: No module named Windows` in pyRevit. CommandLine now falls back to a safe normal WPF window when Revit has not created the requested dockable pane. Ollama endpoint settings ending in `/api` are normalized before the router appends `/api/tags` or `/api/chat`.

## Validation

The workflow runs `test_pyrevit_compat.py`, `test_hybrid_router.py`, `validate_hybrid_release.py`, and Python compilation before building the Windows payload. It then builds the Revit matrix for 2023, 2024, and 2025, publishes both proxies self-contained for `win-x64`, stages the complete installer tree, verifies required files and button assets, compiles Inno Setup, uploads `AuroraRevit-Setup.exe`, and publishes the tagged GitHub Release.

## Workstation automation

`Update-AuroraRevit.ps1` supports release installation and source-only pyRevit deployment. It closes Revit safely, creates a timestamped backup, downloads the official release installer, verifies the installed command files, and restores the backup if deployment fails. Use `-WhatIf` for a dry run and `-Force` only in controlled unattended deployment.

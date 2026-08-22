# AuroraRevit

An AI Assistant Add-in for Autodesk Revit 2023, 2024, and 2025 with 101 embedded discipline examples, OpenAI Cloud routing, and direct Ollama Local support.

## Current release: v2.0.0 Unified Hybrid AI

The latest release is **[AuroraRevit v2.0.0 — Unified Hybrid AI](https://github.com/MazenMostafa2015/AuroraRevit/releases/tag/v2.0.0)**. Download the Windows installer directly from the [release assets](https://github.com/MazenMostafa2015/AuroraRevit/releases/download/v2.0.0/AuroraRevit-Setup.exe). It includes the Revit-tested dockable command-line fix, safe example-preview fallback, improved cancellation handling, individual utility buttons, per-button icons and tooltips, and the unified OpenAI Cloud/Ollama Local provider architecture.

The installer deploys the C# add-in, local proxy, AIChat compatibility button, command tools, utility tools, shared `ai_router.py`, and all ribbon metadata. pyRevit tools are installed to `%APPDATA%\\pyRevit\\Extensions\\AuroraRevit.extension\\RevitTools.tab\\AIAssistant.panel`. It creates **AuroraRevit AI (Cloud)**, **AuroraRevit AI (Local)**, **Aurora Command Tools**, and **Aurora Utility Tools** shortcuts. If Ollama is not detected, installation continues and offers the official download page. The tagged [GitHub Actions build](https://github.com/MazenMostafa2015/AuroraRevit/actions) validates the hybrid client, staged extension payload, icons, descriptions, shared router, and XAML before compiling and publishing `AuroraRevit-Setup.exe`.

## One-step installer for published releases

For a standard per-user installation from a published AuroraRevit release, review
or download `Setup-AuroraRevit.ps1` from this repository and run it from Windows
PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\Setup-AuroraRevit.ps1 -RevitVersion 2025
```

The setup script downloads the selected Revit 2025 release asset (default `v1.0.0`), deploys
the add-in and local proxy below `%LOCALAPPDATA%\\AuroraRevit`, creates a per-user
manifest in `%APPDATA%\\Autodesk\\Revit\\Addins\\<year>`, optionally saves the OpenAI
proxy configuration for the current Windows user, and starts the local proxy. It
does not require Administrator rights. The currently published installer targets
**Revit 2025**; build the appropriate version-specific release from source before
deploying AuroraRevit to Revit 2023 or 2024.

> Review the script before running it. The proxy requires an OpenAI API key to
> process AI queries. You may provide it securely when prompted, pass
> `-OpenAiApiKey`, or configure `OpenAI__ApiKey` later for the current user.

This solution uses a hybrid Aurora Relay pattern. OpenAI Cloud requests travel through the local .NET 8 ASP.NET Core proxy over HTTP/SSE. Ollama Local requests travel directly from the Revit add-in or pyRevit router to `http://localhost:11434/api/chat`. Both paths normalize responses into the same typed JSON action contract consumed by the add-in. The shared provider setting is selected by `AURORA_AI_PROVIDER`, Quick Settings JSON, or the dockable pane dropdown.

## Solution layout

| Project | Target | Responsibility |
| --- | --- | --- |
| `RevitAddin` | .NET Framework 4.8 | Revit `IExternalApplication`, dockable WPF pane, version gate, external command, and HTTP client |
| `AiProxy` | .NET 8 | Local ASP.NET Core proxy with OpenAI chat completion, JSON action normalization, localhost CORS, prompt validation, and port fallback |
| `RevitAddin/AuroraHybridClient.cs` | .NET Framework 4.8 / .NET 8 | Provider-neutral client that routes OpenAI through the proxy and Ollama directly through `/api/chat`, with settings and environment-variable resolution |
| `UtilityTools/ai_router.py` | IronPython 2.7 | Standard-library pyRevit router for OpenAI proxy, direct Ollama, and Smart Fallback |
| `AiProxy.Desktop` | .NET 8 WPF | Local proxy GUI with start/stop controls, health status, active endpoint, and live logs |

## Command Tools Edition

The AuroraRevit installer now deploys a complete pyRevit command-tools bundle to the current user’s `%APPDATA%\\pyRevit\\Extensions\\AuroraRevit.extension\\RevitTools.tab\\AIAssistant.panel` directory. The bundle is independent of the existing `AIChat.pushbutton` and `QuickCommand.pushbutton` buttons.

| Pushbutton | Purpose |
| --- | --- |
| `CommandLogger.pushbutton` | Scans recent Revit journal files and records command identifiers, Windows user, timestamp, Revit version, and an English translation in `C:\\AuroraRevit_Logs\\CommandLog.xlsx`, with CSV fallback. It tracks journal occurrences so repeated commands are not silently collapsed. |
| `AIChat.pushbutton` | Compatibility chat entry point that uses the shared provider router and preserves the traditional AIChat path. |
| `CommandLine.pushbutton` | Provides an AutoCAD-style dockable command bar with OpenAI Cloud/Ollama Local selection, provider health status, Send, Expand Chat, Show Last Log Entry, Smart Fallback, and read-only generated-code review. |
| `CommandLogViewer.pushbutton` | Shows the latest log rows and opens the log folder for quick inspection. |
| `CommandToolsStatus.pushbutton` | Diagnoses all installed buttons, Revit journal availability, log-folder readiness, and proxy ports 5000/5001. |
| `ElementInspector.pushbutton` | Lets the user pick one element and inspect its read-only identity, category, type/family, coordinates, bounding box, and parameters. Escape is treated as a normal cancellation. |
| `QuickSettings.pushbutton` | Saves provider, selected model, Ollama endpoint, log-folder, and light/dark preference per Windows user in a JSON settings file. |
| `ExportToPDF.pushbutton` | Selects printable sheets/views, shows a Safe Preview, asks for confirmation, and submits the set through Revit `PrintManager` to the configured PDF printer. |
| `ExportCurrentViewPDF.pushbutton` | Exports only the active view after selecting output settings and confirming a Safe Preview. |
| `ExportScheduleExcel.pushbutton` | Exports the active schedule to formatted Excel, with CSV fallback when `openpyxl` is unavailable. |
| `BatchParameterTranslator.pushbutton` | Previews controlled text replacement across writable parameters, then commits only after confirmation. |
| `PerformanceMode.pushbutton` | Applies a reversible lightweight display mode to the active view. |
| `RestorePerformanceMode.pushbutton` | Restores settings saved by Performance Mode. |
| `SmartSafetyDetailer.pushbutton` | Previews and confirms a railing safety detail along a selected floor boundary. |

The three expansion buttons were chosen because they cover common command-tool follow-up actions without granting arbitrary model-write or code-execution privileges. Element Inspector is read-only, Quick Settings changes only user-local preferences, and Export to PDF requires explicit confirmation after previewing the selected views.

Every visible pyRevit button now includes a `bundle.yaml` description and a lightweight `icon.png`, so the ribbon communicates each command’s purpose before it is opened. The old single-menu `UtilityTools.pushbutton` has been removed. Its shared IronPython core is stored in a non-button `UtilityTools` folder, while each utility is independently visible and launchable.

The Windows installer creates **AuroraRevit AI (Cloud)** and **AuroraRevit AI (Local)** launchers, plus **Aurora Command Tools** and **Aurora Utility Tools** log-folder shortcuts. The GitHub Actions workflow stages and verifies every button, icon, tooltip, shared core, provider router, and XAML resource before compiling `AuroraRevit-Setup.exe` and publishing the v2.0.0 release.

### Separate utility pushbuttons

The six utility commands are now independently visible in the `AIAssistant.panel`:


| Command | How to use it |
| --- | --- |
| **Export Current View to PDF** | Select an output folder and filename, choose orientation, paper size, and zoom, review the Safe Preview, then confirm submission through Revit `PrintManager`. |
| **Export Active Schedule to Excel** | Activate a `ViewSchedule`, choose an output path, review row/column counts, and export a formatted workbook with a dark-blue/white header. Falls back to CSV when `openpyxl` is unavailable. |
| **Batch Parameter Translator** | Choose a category, source parameter, target parameter, search text, and replacement text. Review the match count before the transaction updates writable target parameters and logs the operation. |
| **Background Calculation Killer** | Enables a reversible performance mode on the active view using coarse detail and wireframe where supported. It does not pretend to disable unsupported global Revit calculations. |
| **Restore Calculations** | Restores the active-view settings captured by the performance mode command. |
| **Smart Safety Detailer** | Pick a floor/slab, set default 1.2-metre post spacing, review the boundary-length preview, and confirm creation of a railing path using the loaded Revit railing type. |
| **Schedule Export to Excel** | Performs the same active-schedule extraction with a clean, minimally formatted workbook for fast data extraction. |

All model-changing operations are guarded by Safe Preview and explicit confirmation. Read-only inspection and export commands do not modify the model. The railing tool reports that final baluster spacing is controlled by the loaded railing type, preventing a misleading guarantee when a project type has different family constraints.

## Build prerequisites

Build the solution on Windows with Visual Studio 2022, the .NET Framework 4.8 developer pack, and the .NET 8 SDK. The RevitAddin project uses version-specific Nice3point community NuGet packages for RevitAPI and RevitAPIUI, so the GitHub Actions workflow does not require local Revit DLL paths. The matrix selects Revit 2023, 2024, or 2025 with `-p:RevitVersion=2023|2024|2025`.

The authoritative CI workflow is `.github/workflows/build-revit-addin.yml`. It runs on `windows-latest`, restores and builds each supported target, publishes both proxy executables as self-contained `win-x64` outputs, generates the matching `.addin` manifest, stages and verifies the pyRevit command-tools extension, and uploads the release artifact.

## Configure and start the proxy

The project references the official `OpenAI` NuGet package. The checked-in `AiProxy/appsettings.json` contains an empty `OpenAI:ApiKey` value and a configurable model name; it does not contain a credential. For local development, prefer .NET user-secrets:

```powershell
cd .\\AiProxy
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_OPENAI_API_KEY"
dotnet user-secrets set "OpenAI:Model" "gpt-4o-mini"
dotnet run --urls http://localhost:5000
```

The equivalent environment variables are `OpenAI__ApiKey` and `OpenAI__Model`. Do not commit a real key to `appsettings.json`, source code, or the repository. The `OpenAI:ApiKey` value is only used to construct the OpenAI client in memory.

The proxy listens on the local machine at `http://localhost:5000`. CORS permits browser-style requests whose origin is `localhost`, `127.0.0.1`, or `::1`, over HTTP or HTTPS, with any header and method. This is intentionally narrower than allowing arbitrary origins.

## Endpoint contract

The existing JSON endpoint remains available at `POST /api/revit-query`. For incremental AI output, the Revit WPF panel uses `POST /api/revit-query/stream` with the same request body and an SSE response.

### `POST /api/revit-query/stream`

The streaming endpoint returns `Content-Type: text/event-stream`. Each event is a JSON payload on a `data:` line followed by a blank line:

```text
data: {"type":"delta","text":"partial answer"}

data: {"type":"done"}
```

The Revit client appends each `delta.text` to one assistant chat bubble. After `done`, it parses the accumulated JSON so `select` and `code` actions still execute only after the response is complete. Provider failures are returned as `{ "type": "error", "message": "..." }` events.

### `POST /api/revit-query`

Request:

```json
{
  "prompt": "Connection test from Revit"
}
```

The proxy prepends the following system prompt to every request before sending it to OpenAI:

> You are an expert Revit API C# assistant. Use the namespace Autodesk.Revit.DB. If the user asks for a selection, return a JSON object `{ "type": "select", "query": "some filter" }`. If they ask for C# code, return `{ "type": "code", "content": "the code here" }`. If the user asks for information, return `{ "type": "info", "message": "the answer" }`. Do not add markdown formatting to the JSON.

The response is normalized to one of the following JSON objects. Markdown code fences are stripped defensively if a model returns them despite the system instruction.

```json
{ "type": "select", "query": "All walls" }
```

```json
{ "type": "code", "content": "// Revit API C# code" }
```

```json
{ "type": "info", "message": "The answer" }
```

Empty or whitespace-only prompts receive HTTP 400. If the API key is missing, the endpoint returns HTTP 503 with an `info` message. OpenAI request failures return HTTP 502 with an `info` message.

## Revit registration

After building `RevitAddin`, copy `AuroraRevit.addin` and `AuroraRevit.RevitAddin.dll` into the appropriate per-user or all-users Revit Addins directory for the target release. The application checks `ControlledApplication.VersionNumber` at startup and succeeds only for `2023`, `2024`, or `2025`; unsupported versions fail gracefully with a user-facing dialog.

The WPF dockable pane is registered as **Aurora AI Assistant** and now provides a modern dark chat interface. It includes a scrollable `StackPanel` chat history, user and assistant message bubbles, a multi-line prompt `TextBox`, a `Send` button, and an animated `Loading...` indicator. Press **Enter** to send or **Shift+Enter** to insert a new line. While the asynchronous request is in progress, the input and send button are disabled. Proxy failures are rendered as an assistant message instead of crashing the Revit UI.

The `AuroraProxyClient` sends JSON to `http://localhost:5000/api/revit-query` using `HttpClient`. The existing `AuroraQueryCommand` remains available as a simple Revit command-level connection test.

## Generate manifests for Revit 2023–2025

After building the add-in, run the included PowerShell script from Windows:

```powershell
.\\RevitAddin\\Generate-RevitManifests.ps1 \\
  -AssemblyPath "C:\\Path\\To\\AuroraRevit.RevitAddin.dll" \\
  -OutputRoot "C:\\Path\\To\\AuroraRevit\\Deployment"
```

The script creates `Deployment\\Revit2023\\AuroraRevit.addin`, `Deployment\\Revit2024\\AuroraRevit.addin`, and `Deployment\\Revit2025\\AuroraRevit.addin`. Each manifest registers both the application bootstrap and the `AuroraQueryCommand`, and uses the resolved absolute assembly path required for deployment. Copy each generated manifest into the corresponding Revit release’s `Addins\\<version>` directory.

## Response-driven Revit actions

The add-in preserves the raw proxy JSON and checks both the response body and the `response` text for an optional `[EXECUTE_REVIT]` marker. It also accepts a direct typed action response. The currently supported contracts are:

```json
{ "type": "select", "query": "All walls" }
```

and:

```json
{ "type": "code", "content": "// generated C# code" }
```

A `select` action currently supports wall queries. It uses `FilteredElementCollector` with `OfClass(typeof(Wall))` and `WhereElementIsNotElementType()` to collect wall instances in the active document, then highlights them in the Revit selection UI. The complete Revit-side operation, including selection assignment, is enclosed in `using (Transaction tx = new Transaction(doc, "AI Action")) { ... }`.

A `code` action opens a separate WPF code-review window with C# syntax coloring. `Copy Code` transfers the source to the clipboard. `Execute in Python Shell` intentionally performs the same copy-and-review flow rather than launching arbitrary AI-generated code automatically; the user must explicitly paste reviewed code into RevitPythonShell.

## Release build and startup

Run the release build from a Windows Developer PowerShell with Visual Studio/MSBuild, the .NET 8 SDK, and the Autodesk Revit API assemblies installed:

```powershell
.\\Build-Release.ps1 -RevitApiPath "C:\\Program Files\\Autodesk\\Revit 2025"
```

The script creates a solution-level `Release` folder. The Revit add-in assembly and base manifest are placed under `Release\\RevitAddin`; the headless AiProxy publish profile targets Windows x64, self-contained deployment, single-file output, and produces `Release\\AiProxy\\AiProxy.exe`. The GUI is published to `Release\\AiProxyGui\\AuroraRevit.ProxyGui.exe`. The published proxy keeps its configuration file external so the API key is not embedded in either executable.

Start the local proxy with:

```powershell
.\\Release\\Start-AuroraRevit.ps1
```

The script checks whether a .NET 8 runtime is installed, prefers the WPF `AuroraRevit.ProxyGui.exe` when it is present, falls back to a hidden headless `AiProxy.exe`, detects the active endpoint on port 5000 or fallback port 5001, and tells the user to open Revit. Because the published executables are self-contained, the .NET 8 warning is informational unless the artifact is replaced with a framework-dependent build.

## Revit manifest installation paths

The generated manifests are under `Release\\Manifests\\Revit2023`, `Release\\Manifests\\Revit2024`, and `Release\\Manifests\\Revit2025`. Copy each version’s `AuroraRevit.addin` file into the matching Revit Addins directory, alongside the built `AuroraRevit.RevitAddin.dll` referenced by the manifest.

| Revit version | Per-user Addins path | All-users Addins path |
|---|---|---|
| Revit 2023 | `%APPDATA%\\Autodesk\\Revit\\Addins\\2023\\` | `C:\\ProgramData\\Autodesk\\Revit\\Addins\\2023\\` |
| Revit 2024 | `%APPDATA%\\Autodesk\\Revit\\Addins\\2024\\` | `C:\\ProgramData\\Autodesk\\Revit\\Addins\\2024\\` |
| Revit 2025 | `%APPDATA%\\Autodesk\\Revit\\Addins\\2025\\` | `C:\\ProgramData\\Autodesk\\Revit\\Addins\\2025\\` |

For a normal user installation, use the per-user path and copy the matching manifest there. The manifest contains an absolute assembly path generated by `Generate-RevitManifests.ps1`; regenerate the manifests if the release folder is moved. Start `Start-AuroraRevit.ps1` before launching Revit so the WPF chat panel can reach the proxy.

## Example Library

The RevitAddin includes an embedded Example Library under `RevitAddin\\Examples` with four discipline folders: `Architecture`, `Structure`, `MEP`, and `General`. Each folder contains an `examples.json` file with at least 10 objects shaped as `{ "title": "Example Title", "prompt": "The actual prompt text for the AI" }`. The MEP catalog includes **Count All HVAC Ducts**, which fills the prompt box with `Count all HVAC ducts in the active project.`.

The WPF panel loads all embedded examples from assembly resources at startup. The ComboBox displays them as `[Discipline] - [Title]`; selecting an item copies its associated `prompt` into the existing input TextBox. The existing chat history, send button, loading behavior, SSE streaming, and action execution remain unchanged.

## Reusable skills and presentation

The original Revit implementation skill is available at `/home/ubuntu/skills/aurora-revit-ai-development/SKILL.md`. The reusable release-engineering skill created from the command-tools workflow is available at `/home/ubuntu/skills/aurora-pyrevit-release-engineering/SKILL.md`. It covers IronPython 2.7 and WPF auditing, safe Revit transactions, explicit sibling loading, Inno Setup payload integration, CI staging gates, version-conflict handling, GitHub publication, and final release verification.

A summary presentation of the v1.9.1 features and architecture is available from the [AuroraRevit v1.9.1 presentation deck](https://github.com/MazenMostafa2015/AuroraRevit/releases/tag/v1.9.1).

## GitHub Actions CI build

The repository includes `.github/workflows/build-revit-addin.yml`. It runs on `windows-latest` for pushes to `main` and pull requests, builds a matrix for Revit 2023, 2024, and 2025, restores and builds the solution in Release mode, publishes both AiProxy and AiProxy.Desktop as self-contained `win-x64` applications, generates the matching absolute-path manifest, and uploads the Revit assembly, manifests, headless proxy, and GUI proxy as versioned artifacts.

The Revit API references no longer depend on a local Revit installation path. They use the community-maintained Nice3point packages with version-specific conditions: `2023.1.90`, `2024.3.60`, and `2025.4.60` [3] [4] [5]. Revit 2023 and 2024 compile as .NET Framework 4.8. Revit 2025 uses `net8.0-windows` because the corresponding package targets the Revit 2025 .NET 8 API surface; this is why the workflow uses a version matrix rather than forcing one target framework for every release.

The migration also replaces `System.Web.Extensions`/`JavaScriptSerializer` with the cross-target `System.Text.Json` package, preserving the WPF chat, SSE client, action parser, and 40 embedded examples across the matrix.

## Validation status

The source, package reference, configuration template, OpenAI service, CORS policy, and response normalizer have been statically inspected. Live compilation and an authenticated OpenAI request could not be run in this Linux sandbox because the .NET SDK, Autodesk Revit assemblies, and a user-provided API key are unavailable here. Windows/Visual Studio validation should confirm package restore, user-secrets loading, CORS preflight behavior, and the end-to-end Revit action flow.

## References

[1]: https://github.com/openai/openai-dotnet "OpenAI .NET API library"
[2]: https://www.nuget.org/packages/OpenAI/2.13.0 "OpenAI NuGet package 2.13.0"
[3]: https://www.nuget.org/packages/Nice3point.Revit.Api.RevitAPI/2023.1.90 "Nice3point RevitAPI package for Revit 2023"
[4]: https://www.nuget.org/packages/Nice3point.Revit.Api.RevitAPI/2024.3.60 "Nice3point RevitAPI package for Revit 2024"
[5]: https://www.nuget.org/packages/Nice3point.Revit.Api.RevitAPI/2025.4.60 "Nice3point RevitAPI package for Revit 2025"

The proxy follows the official `ChatClient` and asynchronous `CompleteChatAsync` usage described in the OpenAI .NET documentation [1] and references the stable `OpenAI` package version 2.13.0 [2]. The Revit project uses the versioned Nice3point API package releases documented in [3] [4] [5].

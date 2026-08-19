# AuroraRevit QA and DevOps Audit

## Scope

This audit covered the Revit WPF add-in, the .NET 8 AiProxy, the new Windows proxy GUI, the GitHub Actions workflow, release scripts, SSE streaming, response-driven Revit actions, and the 40 embedded discipline examples.

## Improvements implemented

| Area | Change | Result |
|---|---|---|
| Proxy GUI | Added `AiProxy.Desktop`, a .NET 8 WPF application with Running/Stopped/Starting status, start/stop controls, endpoint display, live process logs, health polling, and clean shutdown. | Users can operate the proxy without a black console window. |
| Release publishing | Added the GUI to the solution and to `Build-Release.ps1`, GitHub Actions publishing, and release artifacts as a self-contained `win-x64` executable. | The GUI and headless proxy can both be shipped for a user machine. |
| Revit code UX | Added a `Copy this Execution` button to assistant bubbles for generated C# actions. | Generated code can be copied directly from chat while retaining the review window and explicit non-auto-execution boundary. |
| Prompt hardening | Added whitespace/control-character normalization, empty-input rejection, and a 12,000-character prompt limit. | Invalid input returns a controlled HTTP 400 response. |
| API-key hardening | Added format validation for OpenAI keys, safe configuration messages, and sanitized upstream error messages. | Missing, malformed, or rejected credentials do not crash the proxy or expose sensitive detail. |
| Port resilience | Added automatic selection of port 5000, with fallback to 5001 when occupied. The GUI and startup script report the active endpoint. | Common local port conflicts are handled automatically. |
| Embedded resources | Confirmed `Examples\**\examples.json` is declared as `EmbeddedResource`; no `Content` item is used. | All 40 examples remain assembly resources and are not dependent on publish-time loose files. |
| Cross-target serializer | Replaced `System.Web.Extensions`/`JavaScriptSerializer` with `System.Text.Json`. | The action parser, SSE client, and example loader no longer depend on a framework-only serializer. |
| Dependency injection | Corrected `OpenAiChatService` to consume `IOptions<OpenAiOptions>`. | Configuration binding now resolves correctly through ASP.NET Core dependency injection. |

## Virtual compilation test

The requested restore, build, and publish commands were attempted in the sandbox. The sandbox does not contain `dotnet` or `msbuild`, so the commands could not execute locally:

```text
dotnet command unavailable
msbuild command unavailable
restore: SKIPPED
build: SKIPPED
publish: SKIPPED
```

The authoritative compilation path is the Windows GitHub Actions workflow, which uses `windows-latest`, the .NET 8 SDK, the Revit API NuGet packages, and a Revit-version matrix. Static validation passed for the workflow, project references, publish steps, resource declarations, and preserved features.

## Self-audit findings and fixes

The audit found and fixed a runtime dependency-injection defect: `Configure<OpenAiOptions>` registers `IOptions<OpenAiOptions>`, not a plain `OpenAiOptions` object. `OpenAiChatService` now receives `IOptions<OpenAiOptions>` and binds the configured API key and model correctly.

The audit also found a cross-target compilation risk: `System.Web.Extensions` and `JavaScriptSerializer` are not suitable for the Revit 2025 .NET 8 target. All three RevitAddin consumers now use `System.Text.Json`.

A GUI startup defect was corrected: `App.xaml` no longer uses `StartupUri` while also constructing a window in `OnStartup`, preventing duplicate windows. The GUI's published proxy path was corrected to locate `Release\\AiProxy\\AiProxy.exe`, and the GUI now checks both ports before launching.

The original fixed-port behavior was replaced by a resolver that honors an explicit `--urls` argument, uses port 5000 when available, and falls back to 5001. The Revit startup script probes both health endpoints and reports the active URL.

## Remaining environment boundary

Live Windows compilation, WPF rendering, NuGet restore, self-contained publish, Revit API load, and authenticated OpenAI calls must still run in GitHub Actions or on a Windows development machine. The sandbox static checks cannot prove Windows GUI rendering or Revit runtime behavior.

## Final status

The source is ready for the Windows GitHub Actions pipeline. The existing WPF Revit UI, SSE streaming, typed action parsing, transaction-safe Revit operations, and all 40 embedded examples remain present and wired after the QA/DevOps changes.

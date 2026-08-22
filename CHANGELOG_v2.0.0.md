# AuroraRevit v2.0.0 — Unified Hybrid AI

## Summary

AuroraRevit v2.0.0 introduces one provider-neutral architecture for OpenAI Cloud and Ollama Local. Users can switch providers from the C# dockable pane, Quick Settings, or `AURORA_AI_PROVIDER`, while the existing typed Revit action contract remains unchanged.

## Hybrid architecture

- OpenAI Cloud remains behind the local .NET 8 `AiProxy` service. The proxy retains API-key validation, localhost CORS, ports 5000/5001, SSE streaming, prompt validation, and response normalization.
- Ollama Local is queried directly by the Revit add-in through `POST http://localhost:11434/api/chat` and by pyRevit through the shared standard-library `UtilityTools/ai_router.py` module.
- The C# `AuroraHybridClient` selects the provider, reads per-user settings, honors `AURORA_AI_PROVIDER` and `AURORA_OLLAMA_ENDPOINT`, and reports provider health in the dockable UI.
- OpenAI model selection is forwarded per request to the proxy. Ollama model and endpoint selection are read from the same Quick Settings JSON contract.

## UI changes

- Added an **AI Provider** dropdown to the Aurora dockable pane with **OpenAI Cloud** and **Ollama Local** options.
- Added provider-aware health text and failure messages.
- Preserved the existing dark/light theme, chat history, code review, copy execution, example gallery, and Revit action handling.
- Added the missing `AIChat.pushbutton` compatibility entry point so legacy pyRevit paths use the same router as the dockable command bar.

## pyRevit changes

- Added `UtilityTools/ai_router.py` for OpenAI proxy routing, direct Ollama routing, and Smart Fallback.
- Smart Fallback tries the alternate provider when the selected provider is unavailable and clearly marks the response with `providerFallback` and `providerNote`.
- Existing utility buttons remain independent and continue to use Safe Preview and confirmation boundaries for model-changing operations.
- Quick Settings now persists provider, OpenAI model, Ollama model, Ollama endpoint, log folder, and theme.

## Installer and shortcuts

The unified Inno Setup package includes the Revit add-in, proxy, desktop proxy host, AIChat compatibility button, CommandLine, CommandLogger, all command utilities, the shared router, icons, descriptions, and XAML resources.

The installer creates:

- **AuroraRevit AI (Cloud)** — opens the local OpenAI proxy host.
- **AuroraRevit AI (Local)** — starts Ollama when installed or opens the official Ollama download page when it is missing.
- **Aurora Command Tools** — opens `C:\AuroraRevit_Logs`.
- **Aurora Utility Tools** — opens `C:\AuroraRevit_Logs`.

Ollama absence is non-fatal. Installation continues normally and the user can install Ollama later.

## Validation

- All pyRevit scripts and `ai_router.py` passed Python AST parsing and compilation.
- XAML resources passed XML parsing and provider-control assertions.
- C# source passed structural brace and contract checks in the sandbox.
- The validator confirmed 14 visible pyRevit buttons, per-button `icon.png` and `bundle.yaml`, the shared router, provider UI, installer shortcuts, workflow staging, version alignment, and README documentation.
- A local .NET build was not available in the Linux sandbox; the Windows GitHub Actions matrix remains the authoritative C# and Inno Setup build gate.

## Migration notes

Existing users can keep their OpenAI proxy setup unchanged. To choose Ollama Local, install Ollama for Windows, pull a model such as `llama3.2`, then select **Ollama Local** in the Aurora pane or Quick Settings. The default provider remains OpenAI Cloud for compatibility.

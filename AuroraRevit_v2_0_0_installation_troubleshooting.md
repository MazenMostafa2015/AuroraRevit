# AuroraRevit v2.0.0: Troubleshooting and Offline Ollama Guide

## Purpose

This guide covers the most common installation and first-run failures for AuroraRevit v2.0.0. It also explains how to configure Ollama Local for an offline workflow after the installer, models, and provider configuration have been prepared.

> **Important distinction:** “Offline use” means that inference runs against a model already stored on the workstation. The initial Ollama installer and model download require network access unless your organization provides an approved offline software and model transfer process.

## 1. Installation troubleshooting

| Symptom | Likely cause | Recommended resolution |
|---|---|---|
| Windows blocks `AuroraRevit-Setup.exe` | SmartScreen or an unrecognized downloaded executable | Confirm that the file came from the official [AuroraRevit v2.0.0 release](https://github.com/MazenMostafa2015/AuroraRevit/releases/tag/v2.0.0). If the file was downloaded by a browser, open **Properties**, review the publisher/source information available on the machine, and only then use **Unblock** if your organization permits it. |
| Installer cannot copy files | Revit, pyRevit, or another process has files open; permissions are restricted | Close Revit and related file explorers. Right-click the installer and choose **Run as administrator**. If the device is managed, ask IT to permit installation into the selected application and user-profile locations. |
| Setup completes but no AuroraRevit panel appears | Revit did not reload pyRevit or the extension was installed for a different Windows user | Close and reopen Revit. Confirm the extension exists below `%APPDATA%\pyRevit\Extensions\AuroraRevit.extension`. If multiple Windows accounts use the computer, install while signed in as the intended user. |
| Some pyRevit buttons are missing | Partial extension copy, stale pyRevit cache, or an older extension with the same name | Close Revit, verify that `AIChat.pushbutton`, `CommandLine.pushbutton`, `CommandLogger.pushbutton`, and the utility folders exist under the AuroraRevit extension, then restart Revit. Avoid deleting files from another extension unless your administrator has approved it. |
| Desktop shortcuts are missing | Installer was run without shell integration or shortcut creation was restricted | Check the Start Menu group and the installer destination. Re-run Setup if permitted. The key shortcuts are **AuroraRevit AI (Cloud)**, **AuroraRevit AI (Local)**, **Aurora Command Tools**, and **Aurora Utility Tools**. |
| `Aurora Command Tools` opens an empty folder | The log directory has not received any entries yet | Confirm that `C:\AuroraRevit_Logs` exists. Run a harmless test command, then reopen the folder. Do not treat an empty initial folder as an installation failure. |
| Revit reports a DLL load or version error | The add-in build does not match the installed Revit version or a previous DLL is still loaded | Use the v2.0.0 installer, confirm the supported Revit version is 2023, 2024, or 2025, close all Revit processes, and reinstall. Do not mix DLLs manually between version folders. |

## 2. OpenAI Cloud troubleshooting

AuroraRevit’s OpenAI Cloud path uses the local Aurora proxy rather than sending requests directly from the Revit UI. Start **AuroraRevit AI (Cloud)** and wait for the proxy window or process to initialize before testing a prompt.

If the pane reports that the proxy is unavailable, first confirm that the Cloud shortcut points to the installed proxy GUI. Then check whether another process is already using the configured local ports, whether Windows Firewall has blocked the local proxy, and whether the proxy has a valid provider configuration. Review `C:\AuroraRevit_Logs` for the captured error and retry with a short prompt.

If Cloud mode is not required, switch the provider to **Ollama Local** and test a model already installed on the workstation. This separates an OpenAI credential/proxy problem from an AuroraRevit installation problem.

## 3. Configure Ollama Local for offline use

### Initial preparation while online

Install Ollama for Windows from the [official Windows download](https://ollama.com/download/windows). Ollama runs as a native Windows application and serves its local API at `http://localhost:11434` by default.[1] The Windows documentation states that the installer does not require administrator rights and that model storage may require substantially more disk space than the application itself.[1]

Open PowerShell and download a model that your workstation can run. For example:

```powershell
ollama pull llama3.2
```

Confirm that the model is present:

```powershell
ollama list
Invoke-WebRequest -Uri http://localhost:11434/api/tags | Select-Object -ExpandProperty Content
```

The `/api/tags` endpoint returns the locally available model list.[2] Record the exact model name shown by `ollama list`; AuroraRevit must use that name, including any tag suffix.

### Optional: move models to a larger local drive

Ollama’s Windows documentation supports changing the model directory with the user environment variable `OLLAMA_MODELS`.[1] Create a directory such as `D:\OllamaModels`, set the variable for the intended Windows user, quit Ollama from the system tray, and relaunch Ollama so the setting is applied.

```powershell
[Environment]::SetEnvironmentVariable('OLLAMA_MODELS', 'D:\OllamaModels', 'User')
```

If models were already downloaded, use an approved copy/migration process before deleting the original model directory. Keep sufficient free space for the model files and temporary inference data.

### Prepare the offline workstation

Before disconnecting the workstation from the network, complete the following sequence:

| Check | Expected result |
|---|---|
| Ollama application | Installed and able to run from the Start Menu or `ollama.exe` |
| Model | `ollama list` shows the exact model AuroraRevit will use |
| Local API | `Invoke-WebRequest http://localhost:11434/api/tags` returns JSON |
| AuroraRevit | Provider is set to **Ollama Local** in the dockable pane or Quick Settings |
| Test prompt | A short prompt returns a response without network access |
| Fallback setting | Smart Fallback is understood: if the alternate provider is unavailable offline, it cannot produce a remote response |

### Configure AuroraRevit

Open Revit and select **Ollama Local** from the AuroraRevit provider dropdown. In Quick Settings, set the Ollama model to the exact model name returned by `ollama list`. Keep the endpoint at `http://localhost:11434` unless Ollama has been deliberately configured to listen elsewhere. AuroraRevit’s direct local route uses the Ollama chat API at `/api/chat`, while the pyRevit router uses the same local service.

Run a small test such as “Summarize the current selection in one sentence.” Confirm that the provider status reports Ollama Local and that the response does not depend on the OpenAI proxy. For an offline-only workstation, do not select Smart Fallback as a substitute for local availability; fallback to OpenAI Cloud necessarily requires connectivity and valid Cloud configuration.

## 4. Ollama offline failure modes

| Symptom | Diagnosis | Fix |
|---|---|---|
| `localhost:11434` refuses the connection | Ollama is not running or the local service failed to start | Start Ollama from the Start Menu, use the **AuroraRevit AI (Local)** shortcut, or run `ollama serve` in PowerShell. Retry `/api/tags`. |
| API responds but no model is listed | The model was never pulled, was moved without updating `OLLAMA_MODELS`, or the wrong Windows user is running Ollama | Run `ollama list`, verify the environment variable for the active user, relaunch Ollama, and confirm the model directory exists. |
| AuroraRevit says the model is unavailable | The configured model name does not match the installed name | Copy the exact name from `ollama list` into Quick Settings, including the tag. |
| First prompt is very slow | The model is loading into memory or the machine is using CPU inference | Wait for the first request to complete, close memory-heavy applications, and use a smaller model if necessary. A GPU is helpful but not mandatory. |
| Ollama starts then stops | GPU driver, permissions, disk, or runtime issue | Review `%LOCALAPPDATA%\Ollama\server.log`, confirm free disk space, restart the application, and test CPU-compatible operation. |
| Responses fail only in Revit | Provider selection, endpoint, or local firewall configuration is wrong | Test `/api/tags` outside Revit, confirm the provider is Ollama Local, then retry a short prompt. Inspect Aurora logs if the API works independently. |
| Offline machine unexpectedly tries Cloud | Smart Fallback or provider setting still points to OpenAI | Set the provider explicitly to Ollama Local and disable any Cloud fallback policy used by the organization. |

## 5. Logs and diagnostics

Ollama’s Windows logs are commonly located below `%LOCALAPPDATA%\Ollama`; the official troubleshooting documentation identifies `server.log` as the main server log and `%HOMEPATH%\.ollama` as the model/configuration location.[3] AuroraRevit logs are available from **Aurora Command Tools** and are stored in `C:\AuroraRevit_Logs`.

For a controlled diagnostic test, run the following commands and save the output for support:

```powershell
ollama --version
ollama list
(Invoke-WebRequest -Uri http://localhost:11434/api/tags).Content
Get-ChildItem "$env:LOCALAPPDATA\Ollama" -Force
```

Do not publish API keys, internal prompts, project files, or private model data in a support ticket. Redact sensitive paths and content before sharing logs.

## 6. Clean reinstall procedure

If the installation remains inconsistent, close Revit and Ollama, preserve `C:\AuroraRevit_Logs`, uninstall AuroraRevit from Windows Apps if available, and reinstall the official v2.0.0 package. Reinstalling AuroraRevit does not replace the need to preserve Ollama models separately. If `OLLAMA_MODELS` points to a custom directory, confirm that directory before removing anything; the official Windows documentation notes that changed model locations are not removed by the Ollama uninstaller.[1]

After reinstalling, restart Windows or at least restart Revit, pyRevit, and Ollama. Validate the local API, then test AuroraRevit with a short prompt before restoring more advanced workflows.

## References

[1]: https://docs.ollama.com/windows "Ollama Windows documentation"
[2]: https://docs.ollama.com/api/tags "Ollama List Models API"
[3]: https://docs.ollama.com/troubleshooting "Ollama troubleshooting documentation"
[4]: https://docs.ollama.com/api/chat "Ollama Chat API"
[5]: https://github.com/MazenMostafa2015/AuroraRevit/releases/tag/v2.0.0 "AuroraRevit v2.0.0 GitHub Release"

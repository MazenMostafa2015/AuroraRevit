# Revit 2025 crash findings

Source files supplied by the user:
- `pasted_file_vnoWHh_journal.0059.txt`
- `pasted_file_DPz0RC_journal.0059.txt.copy.txt`
- `pasted_file_PF29nc_journal.0059.txt.copy.txt.abbrev`
- `pasted_file_LVJVuk_journal.0059.worker1.log`
- `pasted_file_eX0Ynr_journal.0059.0001.dmp`

Key evidence from the main journal:
- Revit build is `2025.4.6`, branch `RELEASE_2025.4.6`.
- Aurora application and command were registered successfully with `Rvt.Attr.AddInLoadFailureMessage: NoError`.
- The journal records an API assembly warning: `System.Text.Json` version `9.0.0.0` in `AuroraRevit.RevitAddin.dll` conflicts with preloaded `System.Text.Json` version `8.0.0.0`.
- Immediately before the crash, the journal records many pyRevit `ScriptExecutorExternalEventHandler` executions.
- The crash is logged as `ExceptionCode=0xe0434352`, a managed CLR exception, followed by worker shutdown and `unhandledExceptionFilter is executed in a non-main thread`.
- The worker log reports invalid add-in manifest messages for other manifests (`Version number '' is invalid`) but the Aurora manifest itself is listed with `NoError` in the main journal.
- No Aurora schedule execution line or CreateSchedule line appears in the supplied journal text, so the exact HVAC action call is not directly recorded.

Most actionable hypothesis: Aurora is loading but its `System.Text.Json` 9 dependency conflicts with Revit 2025's already-loaded `System.Text.Json` 8. The request may trigger code paths that expose the assembly conflict; pyRevit is also active and repeatedly executing external events, so the supplied logs do not prove Aurora alone caused the worker failure.

Potential hardening direction: pin the Revit 2025 add-in to a Revit-compatible System.Text.Json version or avoid shipping/loading the conflicting package; add safe schedule-category preflight and stronger action logging; reproduce with pyRevit temporarily disabled to separate the two external-event systems.

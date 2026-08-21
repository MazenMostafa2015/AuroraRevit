# AuroraRevit v1.9.2 audit findings

## Reported defects

1. `CommandLine.pushbutton` showed `The requested dockable pane has not been created yet. Parameter name: id`. The current code attempted registration, ignored registration failure, then always called `open_dockable_panel`; it also used a relative XAML filename and called `get_dockable_panel` unnecessarily.
2. The example gallery displayed `No code template is available for this legacy example.` because many embedded JSON entries contain a prompt but no `codeTemplate` property. The C# selection handler used a null fallback string rather than a safe review scaffold.
3. Element Inspector treated pyRevit's normal Escape message `The user aborted the pick operation.` as a failure because it only matched `cancel`.
4. UtilityTools was implemented as one menu button; the requested UX is separate visible buttons with individual icons and descriptions.

## Verified pyRevit API guidance

The official forms reference documents `WPFPanel` class attributes `panel_id`, `panel_source`, and `panel_title`, and the lifecycle `forms.register_dockable_panel(MyPanel)`, `forms.open_dockable_panel(MyPanel)`, and `forms.get_dockable_panel(MyPanel)`. `register_dockable_panel` returns the live panel instance; `open_dockable_panel` is intended only after registration. Source: https://docs.pyrevitlabs.io/reference/pyrevit/forms/ (official pyRevit forms reference; accessed during v1.9.2 audit).

The fix will use an absolute XAML path, preserve registration exceptions, and stop before opening if registration fails. Separate visible pushbuttons will wrap shared utility functions in a non-button `UtilityTools` module.

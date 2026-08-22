# -*- coding: utf-8 -*-
"""Aurora hybrid provider settings for the pyRevit command tools."""
from __future__ import print_function

import json
import os

try:
    from pyrevit import forms
except Exception:
    forms = None

DEFAULT_LOG_DIR = r"C:\AuroraRevit_Logs"
CONFIG_DIR = os.path.join(os.environ.get("APPDATA", DEFAULT_LOG_DIR), "AuroraRevit")
CONFIG_PATH = os.path.join(CONFIG_DIR, "command_tools_settings.json")
DEFAULTS = {
    "provider": "openai",
    "model": "gpt-4o-mini",
    "openai_model": "gpt-4o-mini",
    "ollama_model": "llama3.2",
    "ollama_endpoint": "http://localhost:11434",
    "log_folder": DEFAULT_LOG_DIR,
    "theme": "dark",
}


def _load():
    result = dict(DEFAULTS)
    try:
        with open(CONFIG_PATH, "r") as handle:
            loaded = json.load(handle)
        if isinstance(loaded, dict):
            result.update(loaded)
    except Exception:
        pass
    if not result.get("openai_model"):
        result["openai_model"] = result.get("model", DEFAULTS["openai_model"])
    if not result.get("ollama_model"):
        result["ollama_model"] = DEFAULTS["ollama_model"]
    return result


def _save(settings):
    if not os.path.isdir(CONFIG_DIR):
        os.makedirs(CONFIG_DIR)
    with open(CONFIG_PATH, "w") as handle:
        json.dump(settings, handle, indent=2)


def _valid_folder(value):
    value = os.path.expandvars(os.path.expanduser(value.strip()))
    return os.path.isabs(value), os.path.normpath(value)


def main():
    if not forms:
        return
    settings = _load()
    xaml = """
<Window xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Title=\"Aurora Hybrid Settings\" Width=\"580\" Height=\"470\" Background=\"#FF1E1E1E\" Foreground=\"White\">
  <Grid Margin=\"16\">
    <Grid.RowDefinitions><RowDefinition Height=\"Auto\"/><RowDefinition Height=\"Auto\"/><RowDefinition Height=\"Auto\"/><RowDefinition Height=\"Auto\"/><RowDefinition Height=\"Auto\"/><RowDefinition Height=\"Auto\"/><RowDefinition Height=\"Auto\"/><RowDefinition Height=\"*\"/><RowDefinition Height=\"Auto\"/></Grid.RowDefinitions>
    <TextBlock Grid.Row=\"0\" Text=\"AI provider\" Margin=\"0,0,0,4\" />
    <ComboBox Name=\"ProviderBox\" Grid.Row=\"1\" Height=\"28\" Background=\"#FF2D2D2D\" Foreground=\"White\"><ComboBoxItem Content=\"openai\"/><ComboBoxItem Content=\"ollama\"/></ComboBox>
    <TextBlock Grid.Row=\"2\" Text=\"Selected provider model\" Margin=\"0,10,0,4\" />
    <TextBox Name=\"ModelBox\" Grid.Row=\"3\" Height=\"28\" Background=\"#FF2D2D2D\" Foreground=\"White\" />
    <TextBlock Grid.Row=\"4\" Text=\"Ollama endpoint\" Margin=\"0,10,0,4\" />
    <TextBox Name=\"EndpointBox\" Grid.Row=\"5\" Height=\"28\" Background=\"#FF2D2D2D\" Foreground=\"White\" />
    <TextBlock Grid.Row=\"6\" Text=\"Log folder (absolute path)\" Margin=\"0,10,0,4\" />
    <TextBox Name=\"LogFolderBox\" Grid.Row=\"7\" Height=\"28\" Background=\"#FF2D2D2D\" Foreground=\"White\" VerticalAlignment=\"Top\" />
    <StackPanel Grid.Row=\"8\" Orientation=\"Horizontal\" Margin=\"0,14,0,0\">
      <TextBlock Text=\"Theme: \" VerticalAlignment=\"Center\" />
      <ComboBox Name=\"ThemeBox\" Width=\"100\" Height=\"28\" Margin=\"8,0,12,0\"><ComboBoxItem Content=\"dark\"/><ComboBoxItem Content=\"light\"/></ComboBox>
      <Button Name=\"SaveButton\" Content=\"Save Settings\" Width=\"120\" Height=\"30\" Background=\"#FF0078D4\" Foreground=\"White\" Margin=\"0,0,8,0\" />
      <Button Name=\"CancelButton\" Content=\"Cancel\" Width=\"80\" Height=\"30\" Background=\"#FF0078D4\" Foreground=\"White\" />
    </StackPanel>
  </Grid>
</Window>
"""
    window = forms.WPFWindow(xaml, literal_string=True)
    provider = str(settings.get("provider", DEFAULTS["provider"])).lower()
    window.ProviderBox.SelectedIndex = 1 if provider == "ollama" else 0
    window.ModelBox.Text = str(settings.get("ollama_model" if provider == "ollama" else "openai_model", settings.get("model", DEFAULTS["model"])))
    window.EndpointBox.Text = str(settings.get("ollama_endpoint", DEFAULTS["ollama_endpoint"]))
    window.LogFolderBox.Text = str(settings.get("log_folder", DEFAULTS["log_folder"]))
    theme = str(settings.get("theme", DEFAULTS["theme"])).lower()
    window.ThemeBox.SelectedIndex = 1 if theme == "light" else 0

    def save(_sender, _args):
        valid, folder = _valid_folder(str(window.LogFolderBox.Text or ""))
        if not valid:
            forms.alert("Log folder must be an absolute path.", title="Aurora Hybrid Settings")
            return
        selected_provider = "ollama" if window.ProviderBox.SelectedIndex == 1 else "openai"
        selected_model = str(window.ModelBox.Text or DEFAULTS["model"]).strip()
        updated = dict(settings)
        updated.update({
            "provider": selected_provider,
            "model": selected_model,
            "openai_model": selected_model if selected_provider == "openai" else str(settings.get("openai_model", DEFAULTS["openai_model"])),
            "ollama_model": selected_model if selected_provider == "ollama" else str(settings.get("ollama_model", DEFAULTS["ollama_model"])),
            "ollama_endpoint": str(window.EndpointBox.Text or DEFAULTS["ollama_endpoint"]).strip().rstrip("/"),
            "log_folder": folder,
            "theme": "light" if window.ThemeBox.SelectedIndex == 1 else "dark",
        })
        try:
            _save(updated)
            forms.alert("Settings saved. New Aurora windows will use them immediately.", title="Aurora Hybrid Settings")
            window.Close()
        except Exception as error:
            forms.alert("Could not save settings:\n\n" + str(error), title="Aurora Hybrid Settings")

    window.SaveButton.Click += save
    window.CancelButton.Click += lambda sender, args: window.Close()
    window.show_dialog()


if __name__ == "__main__":
    main()

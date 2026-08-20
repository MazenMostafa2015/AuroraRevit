from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).parent
xaml = (root / "RevitAddin" / "AuroraDockablePaneControl.xaml").read_text(encoding="utf-8")
code = (root / "RevitAddin" / "AuroraDockablePaneControl.xaml.cs").read_text(encoding="utf-8")
ET.fromstring(xaml)
for name in ["ArchitectureExamplesList", "StructureExamplesList", "MepExamplesList", "GeneralExamplesList", "ExampleCodePanel", "ExampleCodeTextBox", "CopyExampleCodeButton", "ThemeToggleButton", "FeedbackButton", "ThinkingProgressBar"]:
    assert f'x:Name="{name}"' in xaml, name
for handler in ["ExampleList_SelectionChanged", "CopyExampleCodeButton_Click", "ThemeToggleButton_Click", "FeedbackButton_Click"]:
    assert f"{handler}" in xaml and f"{handler}" in code, handler
assert "IsScheduleAction" in (root / "RevitAddin" / "RevitActionModels.cs").read_text(encoding="utf-8")
assert "CreateSchedule" in (root / "RevitAddin" / "RevitActionHandler.cs").read_text(encoding="utf-8")
proxy_prompt = (root / "AiProxy" / "OpenAiChatService.cs").read_text(encoding="utf-8")
for category in ["ducts", "pipes", "cable_trays"]:
    assert category in proxy_prompt, category
print("v1.8.3 UI/action contract validation passed.")

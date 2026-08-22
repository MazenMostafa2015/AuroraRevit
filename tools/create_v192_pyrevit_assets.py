# -*- coding: utf-8 -*-
"""Generate lightweight, watermark-free pyRevit icons and metadata for Aurora v1.9.2."""
from __future__ import print_function
import os
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "AuroraRevit.extension", "RevitTools.tab", "AIAssistant.panel"))
BUTTONS = {
    "AIChat.pushbutton": ("AI Chat", "Open a quick Aurora chat prompt using the selected OpenAI Cloud or Ollama Local provider."),
    "CommandLogger.pushbutton": ("Command Logger", "Scan recent Revit journals and record command occurrences to Excel/CSV."),
    "CommandLine.pushbutton": ("Command Line", "Open the dockable Aurora AI command bar with proxy status and safe code review."),
    "CommandLogViewer.pushbutton": ("Command Log Viewer", "Review recent command-log entries and open the Aurora log folder."),
    "CommandToolsStatus.pushbutton": ("Command Tools Status", "Check installed tools, journals, log folder, and local proxy ports."),
    "ElementInspector.pushbutton": ("Element Inspector", "Pick an element and inspect identity, type, coordinates, and parameters read-only."),
    "QuickSettings.pushbutton": ("Quick Settings", "Update local AI model, endpoint, log-folder, and theme preferences."),
    "ExportToPDF.pushbutton": ("Export to PDF", "Preview and confirm printable sheet/view exports through Revit PrintManager."),
    "ExportCurrentViewPDF.pushbutton": ("Export Current View PDF", "Export the active view to a configured PDF printer after Safe Preview."),
    "ExportScheduleExcel.pushbutton": ("Export Schedule Excel", "Export the active schedule to formatted Excel or CSV."),
    "BatchParameterTranslator.pushbutton": ("Batch Parameter Translator", "Preview and confirm a controlled text replacement across writable parameters."),
    "PerformanceMode.pushbutton": ("Performance Mode", "Apply a reversible lightweight display mode to the active view."),
    "RestorePerformanceMode.pushbutton": ("Restore Performance Mode", "Restore the active-view display settings saved by Performance Mode."),
    "SmartSafetyDetailer.pushbutton": ("Smart Safety Detailer", "Preview and confirm a railing safety detail along a selected floor boundary."),
}


def _draw_icon(path, index):
    image = Image.new("RGBA", (32, 32), (15, 23, 42, 0))
    draw = ImageDraw.Draw(image)
    accent = (37, 99, 235, 255)
    white = (248, 250, 252, 255)
    draw.rounded_rectangle((1, 1, 30, 30), radius=6, fill=accent)
    draw.rectangle((5, 5, 27, 27), outline=(147, 197, 253, 255), width=1)
    mode = index % 6
    if mode == 0:
        draw.line((8, 10, 24, 10), fill=white, width=2)
        draw.line((8, 16, 24, 16), fill=white, width=2)
        draw.line((8, 22, 19, 22), fill=white, width=2)
    elif mode == 1:
        draw.ellipse((8, 8, 24, 24), outline=white, width=2)
        draw.line((16, 5, 16, 27), fill=white, width=2)
        draw.line((5, 16, 27, 16), fill=white, width=2)
    elif mode == 2:
        draw.rectangle((8, 8, 24, 24), outline=white, width=2)
        draw.line((11, 20, 21, 10), fill=white, width=2)
    elif mode == 3:
        draw.polygon((16, 6, 25, 24, 7, 24), outline=white, fill=None)
        draw.line((16, 12, 16, 19), fill=white, width=2)
        draw.ellipse((15, 21, 17, 23), fill=white)
    elif mode == 4:
        draw.ellipse((8, 8, 24, 24), outline=white, width=2)
        draw.ellipse((13, 13, 19, 19), fill=white)
    else:
        draw.line((8, 16, 24, 16), fill=white, width=2)
        draw.line((16, 8, 16, 24), fill=white, width=2)
        draw.line((10, 10, 22, 22), fill=white, width=2)
        draw.line((22, 10, 10, 22), fill=white, width=2)
    image.save(path)


def main():
    for index, (folder, values) in enumerate(sorted(BUTTONS.items())):
        title, tooltip = values
        directory = os.path.join(ROOT, folder)
        if not os.path.isdir(directory):
            os.makedirs(directory)
        with open(os.path.join(directory, "bundle.yaml"), "w") as stream:
            stream.write("title: {0}\ntooltip: {1}\nauthor: AuroraRevit\n".format(title, tooltip))
        _draw_icon(os.path.join(directory, "icon.png"), index)
    print("Generated metadata and icons for {0} buttons.".format(len(BUTTONS)))


if __name__ == "__main__":
    main()


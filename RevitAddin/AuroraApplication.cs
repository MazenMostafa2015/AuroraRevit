using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

namespace AuroraRevit.RevitAddin
{
    public sealed class AuroraApplication : IExternalApplication
    {
        public static readonly DockablePaneId PaneId = new DockablePaneId(
            new Guid("E9E4C319-C6AC-4F0E-8C4C-6DBBE0C7A4A5"));

        private static readonly HashSet<string> SupportedVersions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "2023", "2024", "2025" };

        public Result OnStartup(UIControlledApplication application)
        {
            var version = application.ControlledApplication.VersionNumber;
            if (!SupportedVersions.Contains(version))
            {
                TaskDialog.Show(
                    "Aurora Revit AI Assistant",
                    $"This add-in supports Revit 2023, 2024, and 2025. The current Revit version ({version}) is not supported.");
                return Result.Failed;
            }

            application.RegisterDockablePane(
                PaneId,
                "Aurora AI Assistant",
                new AuroraDockablePaneProvider());

            AddRibbonButton(application);
            return Result.Succeeded;
        }

        private static void AddRibbonButton(UIControlledApplication application)
        {
            try
            {
                const string tabName = "Aurora AI";
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    // The tab already exists, which is safe during reloads.
                }

                var panel = application.CreateRibbonPanel(tabName, "Aurora Assistant");
                var assemblyPath = typeof(AuroraApplication).Assembly.Location;
                var buttonData = new PushButtonData(
                    "AuroraAICommand",
                    "Aurora\nAI",
                    assemblyPath,
                    typeof(AuroraQueryCommand).FullName);
                var button = panel.AddItem(buttonData) as PushButton;
                if (button != null)
                {
                    button.ToolTip = "Open the Aurora AI Assistant command bar.";
                    button.LongDescription = "Open the compact bottom command bar, browse examples, and send prompts to the local Aurora proxy.";
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine("Aurora ribbon registration failed: " + exception.Message);
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }

    public sealed class AuroraDockablePaneProvider : IDockablePaneProvider
    {
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = new AuroraDockablePaneControl();
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Bottom
            };
        }
    }

}

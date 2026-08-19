using System;
using System.Windows;

namespace AuroraRevit.AiProxy.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (MainWindow is MainWindow window)
        {
            window.StopProxy();
        }

        base.OnExit(e);
    }
}

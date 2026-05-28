using System.Windows;

namespace Luke.Mvux.Wpf;

public static class ApplicationExtensions
{
    public static void UseMvux(this Application app)
    {
        _ = app;
        SelectionSyncManager.Initialize();
    }
}

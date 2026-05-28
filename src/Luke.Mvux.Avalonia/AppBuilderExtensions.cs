using global::Avalonia;

namespace Luke.Mvux.Avalonia;

public static class AppBuilderExtensions
{
    public static AppBuilder UseMvux(this AppBuilder builder)
    {
        _ = builder;
        SelectionSyncManager.Initialize();
        return builder;
    }
}

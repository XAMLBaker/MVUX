using Avalonia;
using Luke.Mvux.Avalonia;

namespace Avalonia.MultiSelection.Sample;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseMvux()
            .UsePlatformDetect()
            .LogToTrace();
}

using System.Windows;
using Luke.Mvux.Wpf;

namespace Wpf.WhenSelected.Sample;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        this.UseMvux();
        base.OnStartup(e);
    }
}

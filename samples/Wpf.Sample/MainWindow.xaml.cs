using System.Windows;

namespace Wpf.Sample;

public partial class MainWindow : Window
{
    private readonly WeatherViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new WeatherViewModel(new FakeWeatherService());
        DataContext = _vm;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _vm.Dispose();
    }
}

using System.Windows;

namespace Wpf.Demo.Sample;

public partial class MainWindow : Window
{
    private readonly OperationsViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new OperationsViewModel(new FakeOperationsService());
        DataContext = _vm;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _vm.Dispose();
    }
}

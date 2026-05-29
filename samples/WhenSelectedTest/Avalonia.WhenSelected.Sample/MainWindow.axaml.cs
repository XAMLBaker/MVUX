using Avalonia.Controls;

namespace Avalonia.WhenSelected.Sample;

public partial class MainWindow : Window
{
    private readonly WhenSelectedViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new WhenSelectedViewModel();
        DataContext = _vm;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _vm.Dispose();
    }
}

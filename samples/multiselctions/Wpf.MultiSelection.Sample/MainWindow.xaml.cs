using System.Windows;

namespace Wpf.MultiSelection.Sample;

public partial class MainWindow : Window
{
    private readonly MultiSelectionViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MultiSelectionViewModel();
        DataContext = _vm;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _vm.Dispose();
    }
}

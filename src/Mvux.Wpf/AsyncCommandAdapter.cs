using Mvux.Wpf.Core;
using System.Windows.Input;

namespace Mvux.Wpf;

/// <summary>
/// Wraps IAsyncCommand as WPF ICommand so it can be bound to Button.Command.
/// </summary>
public sealed class AsyncCommandAdapter : ICommand
{
    private readonly IAsyncCommand _inner;

    public AsyncCommandAdapter(IAsyncCommand inner)
    {
        _inner = inner;
        _inner.CanExecuteChanged += (s, e) => CanExecuteChanged?.Invoke(s, e);
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _inner.CanExecute();

    public void Execute(object? parameter) => _ = _inner.ExecuteAsync();
}

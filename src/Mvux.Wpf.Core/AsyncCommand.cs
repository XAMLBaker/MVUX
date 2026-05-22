using System.Windows.Input;

namespace Mvux.Wpf.Core;

public interface IAsyncCommand
{
    bool CanExecute();
    Task ExecuteAsync();
    event EventHandler? CanExecuteChanged;
}

public sealed class AsyncCommand(Func<Task> execute) : IAsyncCommand, ICommand
{
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute() => !_isExecuting;

    bool ICommand.CanExecute(object? parameter) => CanExecute();
    void ICommand.Execute(object? parameter) => _ = ExecuteAsync();

    public async Task ExecuteAsync()
    {
        if (_isExecuting) return;
        _isExecuting = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await execute();
        }
        finally
        {
            _isExecuting = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

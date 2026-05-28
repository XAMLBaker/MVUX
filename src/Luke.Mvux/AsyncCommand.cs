using System.Windows.Input;

namespace Luke.Mvux;

public interface IAsyncCommand : ICommand
{
    bool IsExecuting { get; }
    Task ExecuteAsync(object? parameter = null);
}

public sealed class AsyncCommand(
    Func<object?, CancellationToken, ValueTask> execute,
    Func<object?, bool>? canExecute = null) : IAsyncCommand
{
    private readonly Func<object?, CancellationToken, ValueTask> _execute = execute;
    private readonly Func<object?, bool> _canExecute = canExecute ?? (_ => true);
    private readonly CancellationTokenSource _cts = new();
    private int _isExecuting;

    public event EventHandler? CanExecuteChanged;
    public bool IsExecuting => Volatile.Read(ref _isExecuting) == 1;

    public bool CanExecute(object? parameter) => !IsExecuting && _canExecute(parameter);

    public void Execute(object? parameter) => _ = ExecuteAsync(parameter);

    public async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter))
            return;

        if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
            return;

        CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            await _execute(parameter, _cts.Token);
        }
        finally
        {
            Interlocked.Exchange(ref _isExecuting, 0);
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Cancel() => _cts.Cancel();
}

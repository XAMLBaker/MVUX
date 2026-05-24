using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Luke.Mvux.Avalonia;

public sealed class FeedViewState : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private object? _data;
    private Exception? _error;
    private bool _progress;
    private object? _parent;

    public object? Data
    {
        get => _data;
        internal set { _data = value; Notify(); }
    }

    public Exception? Error
    {
        get => _error;
        internal set { _error = value; Notify(); }
    }

    public bool Progress
    {
        get => _progress;
        internal set { _progress = value; Notify(); }
    }

    public ICommand? Refresh { get; internal set; }

    public object? Parent
    {
        get => _parent;
        internal set { _parent = value; Notify(); }
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

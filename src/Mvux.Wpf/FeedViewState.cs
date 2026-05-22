using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Mvux.Wpf;

public sealed class FeedViewState : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private object? _data;
    private Exception? _error;
    private bool _progress;
    private object? _parent;

    /// <summary>실제 피드 데이터. 데이터 있을 때만 non-null.</summary>
    public object? Data
    {
        get => _data;
        internal set { _data = value; Notify(); }
    }

    /// <summary>피드가 에러 상태일 때의 예외. Error.Message 바인딩 가능.</summary>
    public Exception? Error
    {
        get => _error;
        internal set { _error = value; Notify(); }
    }

    /// <summary>피드가 로딩 중이면 true.</summary>
    public bool Progress
    {
        get => _progress;
        internal set { _progress = value; Notify(); }
    }

    /// <summary>피드를 재로드하는 커맨드. ErrorTemplate에서 Retry 버튼 등에 바인딩.</summary>
    public ICommand? Refresh { get; internal set; }

    /// <summary>FeedView의 DataContext (부모 ViewModel). Parent.SomeProperty 형태로 바인딩 가능.</summary>
    public object? Parent
    {
        get => _parent;
        internal set { _parent = value; Notify(); }
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

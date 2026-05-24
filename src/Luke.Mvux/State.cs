using Luke.Mvux.Internal;

namespace Luke.Mvux;

public static class State
{
    public static IState<T> Value<T>(T initialValue)
        => new StateImpl<T>(Option<T>.Some(initialValue));

    public static IState<T> Empty<T>()
        => new StateImpl<T>(Option<T>.None());

    public static IState<T> Async<T>(Func<CancellationToken, ValueTask<T>> fetch)
        => new AsyncState<T>(fetch);

    public static IState<T> Async<T>(Func<CancellationToken, Task<T>> fetch)
        => new AsyncState<T>(ct => new ValueTask<T>(fetch(ct)));
}

using Luke.Mvux.Internal;
using System.Runtime.CompilerServices;

namespace Luke.Mvux;

public static class State<T>
{
    public static IState<T> Empty<TOwner>(TOwner owner, [CallerMemberName] string? name = null, [CallerLineNumber] int line = -1)
        where TOwner : class
        => AttachedProperty.GetOrCreate(owner, (name!, line), static (o, _) => (IState<T>)new StateImpl<T>(Option<T>.None()));

    public static IState<T> Value<TOwner>(TOwner owner, Func<T> valueProvider)
        where TOwner : class
        => AttachedProperty.GetOrCreate(owner, valueProvider, static (o, vp) => (IState<T>)new StateImpl<T>(Option<T>.Some(vp())));

    public static IState<T> Async<TOwner>(TOwner owner, Func<CancellationToken, ValueTask<T>> fetch)
        where TOwner : class
        => AttachedProperty.GetOrCreate(owner, fetch, static (o, f) => (IState<T>)new AsyncState<T>(f));

    public static IState<T> Async<TOwner>(TOwner owner, Func<CancellationToken, Task<T>> fetch)
        where TOwner : class
        => AttachedProperty.GetOrCreate(owner, fetch, static (o, f) => (IState<T>)new AsyncState<T>(ct => new ValueTask<T>(f(ct))));
}

public static class State
{
    // ── owner 패턴 (T 추론 가능한 경우) ─────────────────────────────────────

    public static IState<TValue> Value<TOwner, TValue>(TOwner owner, Func<TValue> valueProvider)
        where TOwner : class
        => State<TValue>.Value(owner, valueProvider);

    public static IState<TValue> Async<TOwner, TValue>(TOwner owner, Func<CancellationToken, ValueTask<TValue>> fetch)
        where TOwner : class
        => State<TValue>.Async(owner, fetch);

    public static IState<TValue> Async<TOwner, TValue>(TOwner owner, Func<CancellationToken, Task<TValue>> fetch)
        where TOwner : class
        => State<TValue>.Async(owner, fetch);

    // ── 기존 API (owner 없음) ────────────────────────────────────────────────

    public static IState<T> Value<T>(T initialValue)
        => new StateImpl<T>(Option<T>.Some(initialValue));

    public static IState<T> Empty<T>()
        => new StateImpl<T>(Option<T>.None());

    public static IState<T> Async<T>(Func<CancellationToken, ValueTask<T>> fetch)
        => new AsyncState<T>(fetch);

    public static IState<T> Async<T>(Func<CancellationToken, Task<T>> fetch)
        => new AsyncState<T>(ct => new ValueTask<T>(fetch(ct)));
}

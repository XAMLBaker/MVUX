using Luke.Mvux.Internal;
using System.Runtime.CompilerServices;

namespace Luke.Mvux;

public static class ListState<T>
{
    public static IListState<T> Empty<TOwner>(TOwner owner, [CallerMemberName] string? name = null, [CallerLineNumber] int line = -1)
        where TOwner : class
        => AttachedProperty.GetOrCreate(owner, (name!, line), static (o, _) => (IListState<T>)new ListStateImpl<T>());

    public static IListState<T> Value<TOwner>(
        TOwner owner,
        Func<IEnumerable<T>> valueProvider,
        [CallerMemberName] string? name = null,
        [CallerLineNumber] int line = -1)
        where TOwner : class
        => AttachedProperty.GetOrCreate(owner, (name!, line), (o, _) => (IListState<T>)new ListStateImpl<T>(valueProvider()));
}

public static class ListState
{
    // ── owner 패턴 (T 추론 가능한 경우) ─────────────────────────────────────

    public static IListState<TValue> Value<TOwner, TValue>(
        TOwner owner,
        Func<IEnumerable<TValue>> valueProvider,
        [CallerMemberName] string? name = null,
        [CallerLineNumber] int line = -1)
        where TOwner : class
        => ListState<TValue>.Value(owner, valueProvider, name, line);

    // ── 기존 API (owner 없음) ────────────────────────────────────────────────

    public static IListState<T> Empty<T>()
        => new ListStateImpl<T>();

    public static IListState<T> Value<T>(IEnumerable<T> items)
        => new ListStateImpl<T>(items);
}

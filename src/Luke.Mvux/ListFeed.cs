using Luke.Mvux.Internal;

namespace Luke.Mvux;

public static class ListFeed
{
    public static IListFeed<T> Async<T>(Func<CancellationToken, Task<IReadOnlyList<T>>> fetch)
        => new AsyncListFeed<T>(fetch);

    public static IListFeed<T> Async<T>(Func<CancellationToken, ValueTask<IReadOnlyList<T>>> fetch)
        => new AsyncListFeed<T>(ct => fetch(ct).AsTask());

    public static IListFeed<T> AsyncEnumerable<T>(Func<CancellationToken, IAsyncEnumerable<IReadOnlyList<T>>> source)
        => new AsyncEnumerableListFeed<T>(source);
}

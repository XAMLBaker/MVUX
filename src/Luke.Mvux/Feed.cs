using Luke.Mvux.Internal;

namespace Luke.Mvux;

public static class Feed
{
    public static IFeed<T> Async<T>(Func<CancellationToken, ValueTask<T>> fetch)
        => new AsyncFeed<T>(fetch);

    public static IFeed<T> Async<T>(Func<CancellationToken, Task<T>> fetch)
        => new AsyncFeed<T>(ct => new ValueTask<T>(fetch(ct)));
}

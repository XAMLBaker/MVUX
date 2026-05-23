using Lw.Mvux.Internal;

namespace Lw.Mvux;

public static class ListState
{
    public static IListState<T> Empty<T>()
        => new ListStateImpl<T>();

    public static IListState<T> Value<T>(IEnumerable<T> items)
        => new ListStateImpl<T>(items);
}

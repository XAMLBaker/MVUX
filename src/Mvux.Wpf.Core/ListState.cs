using Mvux.Wpf.Core.Internal;

namespace Mvux.Wpf.Core;

public static class ListState
{
    public static IListState<T> Empty<T>()
        => new ListStateImpl<T>();

    public static IListState<T> Value<T>(IEnumerable<T> items)
        => new ListStateImpl<T>(items);
}

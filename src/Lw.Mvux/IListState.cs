namespace Lw.Mvux;

public interface IListState<T> : IListFeed<T>
{
    ValueTask AddAsync(T item, CancellationToken ct = default);
    ValueTask AddRangeAsync(IEnumerable<T> items, CancellationToken ct = default);
    ValueTask InsertAtAsync(int index, T item, CancellationToken ct = default);
    ValueTask RemoveAsync(Func<T, bool> predicate, CancellationToken ct = default);
    ValueTask RemoveAsync(T item, CancellationToken ct = default);
    ValueTask UpdateAsync(Func<IReadOnlyList<T>, IReadOnlyList<T>> updater, CancellationToken ct = default);
    ValueTask UpdateAsync(T oldItem, T newItem, CancellationToken ct = default);
    ValueTask SetAsync(IEnumerable<T> items, CancellationToken ct = default);
    ValueTask ClearAsync(CancellationToken ct = default);
}

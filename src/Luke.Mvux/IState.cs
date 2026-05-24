namespace Luke.Mvux;

public interface IState<T> : IFeed<T>
{
    ValueTask SetAsync(T value, CancellationToken ct = default);
    ValueTask UpdateAsync(Func<Option<T>, Option<T>> updater, CancellationToken ct = default);
    ValueTask UpdateAsync(Func<T?, T?> updater, CancellationToken ct = default);
}

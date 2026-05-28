namespace Luke.Mvux;

public interface ISelectionFeed
{
    bool HasSelection { get; }
    bool SupportsMultipleSelection { get; }
    ValueTask SetSelectedAsync(object? item, CancellationToken ct = default);
    IAsyncEnumerable<IMessage> GetSelectionMessages(CancellationToken ct);
}

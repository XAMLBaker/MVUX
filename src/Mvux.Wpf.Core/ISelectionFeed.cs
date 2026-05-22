namespace Mvux.Wpf.Core;

public interface ISelectionFeed
{
    bool HasSelection { get; }
    ValueTask SetSelectedAsync(object? item, CancellationToken ct = default);
    IAsyncEnumerable<IMessage> GetSelectionMessages(CancellationToken ct);
}

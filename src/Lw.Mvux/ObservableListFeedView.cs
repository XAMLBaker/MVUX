using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Lw.Mvux;

public sealed class ObservableListFeedView<T> : ObservableCollection<T>, ISelectionFeed
{
    private readonly ISelectionFeed? _selFeed;
    private readonly SynchronizationContext _syncContext;

    public ObservableListFeedView(IListFeed<T> source, CancellationToken ct)
    {
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _selFeed = source as ISelectionFeed;
        _ = SubscribeListAsync(source, ct);
        if (_selFeed is { HasSelection: true })
            _ = SubscribeSelectionAsync(_selFeed, ct);
    }

    private async Task SubscribeListAsync(IListFeed<T> source, CancellationToken ct)
    {
        try
        {
            await foreach (var msg in source.GetSource(ct))
            {
                if (ct.IsCancellationRequested) return;
                var captured = msg;
                _syncContext.Post(_ =>
                {
                    if (ct.IsCancellationRequested) return;
                    if (captured.Data.IsSome(out var items)) Refresh(items);
                    else Clear();
                }, null);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task SubscribeSelectionAsync(ISelectionFeed selFeed, CancellationToken ct)
    {
        try
        {
            await foreach (var msg in selFeed.GetSelectionMessages(ct))
            {
                if (ct.IsCancellationRequested) return;
                ObservableListFeedViewConfig.OnSelectionUpdated?.Invoke(this, msg.HasData ? msg.DataObject : null);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void Refresh(IReadOnlyList<T> newItems)
    {
        for (int i = Count - 1; i >= 0; i--)
            if (!newItems.Contains(this[i])) RemoveAt(i);

        for (int i = 0; i < newItems.Count; i++)
        {
            if (i < Count) { if (!Equals(this[i], newItems[i])) SetItem(i, newItems[i]); }
            else Add(newItems[i]);
        }
    }

    bool ISelectionFeed.HasSelection => _selFeed != null;

    ValueTask ISelectionFeed.SetSelectedAsync(object? item, CancellationToken ct)
        => _selFeed?.SetSelectedAsync(item, ct) ?? ValueTask.CompletedTask;

    IAsyncEnumerable<IMessage> ISelectionFeed.GetSelectionMessages(CancellationToken ct)
        => _selFeed?.GetSelectionMessages(ct) ?? EmptyMessages(ct);

    private static async IAsyncEnumerable<IMessage> EmptyMessages(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask;
        yield break;
    }
}

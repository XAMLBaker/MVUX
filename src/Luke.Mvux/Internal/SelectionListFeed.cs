using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Luke.Mvux.Internal;

internal sealed class SelectionListFeed<T>(
    IListFeed<T> source,
    IState<T> selectedItem) : IListFeed<T>, ISelectionFeed
{
    public async IAsyncEnumerable<Message<IReadOnlyList<T>>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var msg in source.GetSource(ct))
        {
            yield return msg;

            if (msg.HasData)
                _ = ClearIfStaleAsync(msg.Data.SomeOrDefault()!, ct);
        }
    }

    bool ISelectionFeed.HasSelection => true;

    bool ISelectionFeed.SupportsMultipleSelection => false;

    ValueTask ISelectionFeed.SetSelectedAsync(object? item, CancellationToken ct)
        => item is T typed ? selectedItem.SetAsync(typed, ct) : selectedItem.SetNoneAsync(ct);

    IAsyncEnumerable<IMessage> ISelectionFeed.GetSelectionMessages(CancellationToken ct)
        => selectedItem.GetMessages(ct);

    private async Task ClearIfStaleAsync(IReadOnlyList<T> list, CancellationToken ct)
    {
        var sel = await selectedItem;
        if (sel is not null && !list.Any(item => Equals(item, sel)))
            await selectedItem.SetNoneAsync(ct);
    }
}

internal sealed class MultiSelectionListFeed<T>(
    IListFeed<T> source,
    IState<ImmutableList<T>> selectedItems) : IListFeed<T>, ISelectionFeed
{
    public async IAsyncEnumerable<Message<IReadOnlyList<T>>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var msg in source.GetSource(ct))
        {
            yield return msg;

            if (msg.HasData)
                _ = PruneStaleAsync(msg.Data.SomeOrDefault()!, ct);
        }
    }

    bool ISelectionFeed.HasSelection => true;

    bool ISelectionFeed.SupportsMultipleSelection => true;

    ValueTask ISelectionFeed.SetSelectedAsync(object? item, CancellationToken ct)
        => selectedItems.SetAsync(ToSelection(item), ct);

    IAsyncEnumerable<IMessage> ISelectionFeed.GetSelectionMessages(CancellationToken ct)
        => selectedItems.GetMessages(ct);

    private async Task PruneStaleAsync(IReadOnlyList<T> list, CancellationToken ct)
    {
        var sel = await selectedItems;
        if (sel is null || sel.Count == 0) return;

        var pruned = sel.RemoveAll(item => !list.Any(x => Equals(x, item)));
        if (pruned.Count != sel.Count)
            await selectedItems.SetAsync(pruned, ct);
    }

    private static ImmutableList<T> ToSelection(object? item)
    {
        if (item is null)
            return ImmutableList<T>.Empty;

        if (item is IEnumerable items && item is not string)
            return items.OfType<T>().ToImmutableList();

        return item is T typed ? ImmutableList.Create(typed) : ImmutableList<T>.Empty;
    }
}

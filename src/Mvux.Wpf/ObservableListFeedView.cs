using System.Collections.ObjectModel;
using System.Windows.Threading;
using Mvux.Wpf.Core;

namespace Mvux.Wpf;

/// <summary>
/// IListFeed를 구독하여 WPF ObservableCollection으로 노출.
/// 소스가 ISelectionFeed이면 SelectionSyncManager를 통해
/// 어떤 Selector에든 ItemsSource 바인딩 하나로 선택 자동 동기화.
/// </summary>
public sealed class ObservableListFeedView<T> : ObservableCollection<T>, ISelectionFeed
{
    private readonly ISelectionFeed? _selFeed;

    public ObservableListFeedView(IListFeed<T> source, CancellationToken ct, Dispatcher dispatcher)
    {
        _selFeed = source as ISelectionFeed;

        SelectionSyncManager.EnsureInitialized();

        _ = SubscribeListAsync(source, ct, dispatcher);

        if (_selFeed is { HasSelection: true })
            _ = SubscribeSelectionAsync(_selFeed, ct);
    }

    // ── List 구독 ─────────────────────────────────────────────────────────────

    private async Task SubscribeListAsync(IListFeed<T> source, CancellationToken ct, Dispatcher dispatcher)
    {
        try
        {
            await foreach (var msg in source.GetSource(ct))
            {
                if (ct.IsCancellationRequested) return;
                var captured = msg;
                dispatcher.Invoke(() =>
                {
                    if (ct.IsCancellationRequested) return;
                    if (captured.Data.IsSome(out var items)) Refresh(items);
                    else Clear();
                });
            }
        }
        catch (OperationCanceledException) { }
    }

    // ── Selection 구독 → 전역 Selector 업데이트 ──────────────────────────────

    private async Task SubscribeSelectionAsync(ISelectionFeed selFeed, CancellationToken ct)
    {
        try
        {
            await foreach (var msg in selFeed.GetSelectionMessages(ct))
            {
                if (ct.IsCancellationRequested) return;
                SelectionSyncManager.UpdateSelection(this, msg.HasData ? msg.DataObject : null);
            }
        }
        catch (OperationCanceledException) { }
    }

    // ── Diff refresh ──────────────────────────────────────────────────────────

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

    // ── ISelectionFeed 포워딩 ─────────────────────────────────────────────────

    bool ISelectionFeed.HasSelection => _selFeed != null;

    ValueTask ISelectionFeed.SetSelectedAsync(object? item, CancellationToken ct)
        => _selFeed?.SetSelectedAsync(item, ct) ?? ValueTask.CompletedTask;

    IAsyncEnumerable<IMessage> ISelectionFeed.GetSelectionMessages(CancellationToken ct)
        => _selFeed?.GetSelectionMessages(ct) ?? EmptyMessages(ct);

    private static async IAsyncEnumerable<IMessage> EmptyMessages(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask;
        yield break;
    }
}

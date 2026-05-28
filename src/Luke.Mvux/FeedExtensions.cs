using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Luke.Mvux.Internal;

namespace Luke.Mvux;

public static class FeedExtensions
{
    // ── Selection ────────────────────────────────────────────────────────────

    public static IListFeed<T> Selection<T>(
        this IListFeed<T> source,
        IState<T> selectedItem)
        => new SelectionListFeed<T>(source, selectedItem);

    public static IListFeed<T> Selection<T>(
        this IListFeed<T> source,
        IState<ImmutableList<T>> selectedItems)
        => new MultiSelectionListFeed<T>(source, selectedItems);

    // ── IFeed composition ────────────────────────────────────────────────────

    public static IFeed<TResult> Select<T, TResult>(
        this IFeed<T> source,
        Func<T, TResult> selector)
        => new SelectFeed<T, TResult>(source, selector);

    public static IFeed<TResult> SelectAsync<T, TResult>(
        this IFeed<T> source,
        Func<T, CancellationToken, ValueTask<TResult>> selector)
        => new SelectAsyncFeed<T, TResult>(source, selector);

    public static IFeed<TResult> SelectAsync<T, TResult>(
        this IFeed<T> source,
        Func<T, CancellationToken, Task<TResult>> selector)
        => new SelectAsyncFeed<T, TResult>(source, async (v, ct) => await selector(v, ct));

    public static IFeed<TResult> SelectAsync<T, TResult>(
        this IFeed<T> source,
        Func<T, Task<TResult>> selector)
        => new SelectAsyncFeed<T, TResult>(source, (v, _) => new ValueTask<TResult>(selector(v)));

    public static IFeed<T> Where<T>(
        this IFeed<T> source,
        Func<T, bool> predicate)
        => new WhereFeed<T>(source, predicate);

    /// <summary>Transforms each item within the list. Returns IListFeed preserving list type.</summary>
    public static IListFeed<TResult> Select<T, TResult>(
        this IListFeed<T> source,
        Func<T, TResult> selector)
        => new SelectListFeed<T, TResult>(source, selector);

    /// <summary>Filters items within each list emission. Returns IListFeed preserving list type.</summary>
    public static IListFeed<T> Where<T>(
        this IListFeed<T> source,
        Func<T, bool> predicate)
        => new WhereListFeed<T>(source, predicate);

    // ── IState<T> helpers ────────────────────────────────────────────────────

    public static ValueTask SetNoneAsync<T>(this IState<T> state, CancellationToken ct = default)
        => state.UpdateAsync(_ => Option<T>.None(), ct);

    /// <summary>Async updater — reads current value, applies async transform, writes back.</summary>
    public static ValueTask UpdateAsync<T>(
        this IState<T> state,
        Func<T?, CancellationToken, ValueTask<T?>> asyncUpdater,
        CancellationToken ct = default)
        => UpdateAsyncCore(state, asyncUpdater, ct);

    private static async ValueTask UpdateAsyncCore<T>(
        IState<T> state,
        Func<T?, CancellationToken, ValueTask<T?>> asyncUpdater,
        CancellationToken ct)
    {
        T? current = await state;
        var result = await asyncUpdater(current, ct);
        await state.UpdateAsync(_ => result, ct);
    }

    // ── IListState<T> helpers ────────────────────────────────────────────────

    /// <summary>Update the first item matching the predicate using an updater function.</summary>
    public static ValueTask UpdateAsync<T>(
        this IListState<T> listState,
        Func<T, bool> predicate,
        Func<T, T> updater,
        CancellationToken ct = default)
        => listState.UpdateAsync(list =>
        {
            var result = list.ToList();
            for (int i = 0; i < result.Count; i++)
                if (predicate(result[i]))
                {
                    result[i] = updater(result[i]);
                    break;
                }
            return result;
        }, ct);

    /// <summary>Update all items matching the predicate using an updater function.</summary>
    public static ValueTask UpdateAllAsync<T>(
        this IListState<T> listState,
        Func<T, bool> predicate,
        Func<T, T> updater,
        CancellationToken ct = default)
        => listState.UpdateAsync(list =>
            list.Select(item => predicate(item) ? updater(item) : item).ToList(), ct);

    /// <summary>Remove the first item equal to the given item (by equality).</summary>
    public static ValueTask RemoveFirstAsync<T>(
        this IListState<T> listState,
        T item,
        CancellationToken ct = default)
        => listState.RemoveAsync(item, ct);

    // ── ForEachAsync ─────────────────────────────────────────────────────────

    public static async ValueTask ForEachAsync<T>(
        this IFeed<T> source,
        Func<T, CancellationToken, ValueTask> action,
        CancellationToken ct)
    {
        await foreach (var msg in source.GetSource(ct))
            if (msg.Data.IsSome(out var value))
                await action(value, ct);
    }

    public static ValueTask ForEachAsync<T>(
        this IFeed<T> source,
        Func<T, ValueTask> action,
        CancellationToken ct)
        => source.ForEachAsync((value, _) => action(value), ct);

    public static ValueTask ForEachAsync<T>(
        this IFeed<T> source,
        Action<T> action,
        CancellationToken ct)
        => source.ForEachAsync((value, _) => { action(value); return ValueTask.CompletedTask; }, ct);

    /// <summary>Iterates each version of the list (called once per list update).</summary>
    public static async ValueTask ForEachAsync<T>(
        this IListFeed<T> source,
        Func<IReadOnlyList<T>, CancellationToken, ValueTask> action,
        CancellationToken ct)
    {
        await foreach (var msg in source.GetSource(ct))
            if (msg.Data.IsSome(out var list))
                await action(list, ct);
    }

    // ── GetAwaiter (await state) ─────────────────────────────────────────────

    public static TaskAwaiter<T?> GetAwaiter<T>(this IState<T> state)
        => GetCurrentAsync(state).GetAwaiter();

    private static async Task<T?> GetCurrentAsync<T>(IState<T> state)
    {
        await foreach (var msg in state.GetSource(CancellationToken.None))
        {
            if (msg.Data.IsSome(out var value))
                return value;

            if (msg.IsNone)
                return default;
        }

        return default;
    }

    public static async ValueTask<T?> GetCurrentAsync<T>(this IFeed<T> feed, CancellationToken ct = default)
    {
        await foreach (var msg in feed.GetSource(ct))
        {
            if (msg.Data.IsSome(out var value))
                return value;

            if (msg.IsNone)
                return default;
        }

        return default;
    }
}

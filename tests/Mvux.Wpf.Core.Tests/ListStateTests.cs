using Mvux.Wpf.Core;

namespace Mvux.Wpf.Core.Tests;

public class ListStateTests
{
    [Fact]
    public async Task ListState_Empty_ReturnsEmptyList()
    {
        var state = ListState.Empty<string>();

        var msg = await FirstAsync(state);

        Assert.True(msg.HasData);
        Assert.Empty(msg.Data.Value);
    }

    [Fact]
    public async Task ListState_Value_ReturnsInitialItems()
    {
        var state = ListState.Value(["Seoul", "Busan"]);

        var msg = await FirstAsync(state);

        Assert.Equal(["Seoul", "Busan"], msg.Data.Value);
    }

    [Fact]
    public async Task ListState_AddAsync_BroadcastsNewList()
    {
        var state = ListState.Empty<string>();
        var collected = await CollectAsync(state, mutate: () => state.AddAsync("Seoul").AsTask(), count: 2);

        Assert.Empty(collected[0]);
        Assert.Equal(["Seoul"], collected[1]);
    }

    [Fact]
    public async Task ListState_AddRangeAsync_AddsMultipleItems()
    {
        var state = ListState.Empty<string>();
        var collected = await CollectAsync(state, mutate: () => state.AddRangeAsync(["Seoul", "Busan"]).AsTask(), count: 2);

        Assert.Empty(collected[0]);
        Assert.Equal(["Seoul", "Busan"], collected[1]);
    }

    [Fact]
    public async Task ListState_InsertAtAsync_InsertsAtCorrectPosition()
    {
        var state = ListState.Value(["Seoul", "Incheon"]);
        var collected = await CollectAsync(state, mutate: () => state.InsertAtAsync(1, "Busan").AsTask(), count: 2);

        Assert.Equal(["Seoul", "Busan", "Incheon"], collected[1]);
    }

    [Fact]
    public async Task ListState_RemoveAsync_Predicate_RemovesMatchingItems()
    {
        var state = ListState.Value(["Seoul", "Busan", "Incheon"]);
        var collected = await CollectAsync(state, mutate: () => state.RemoveAsync(x => x == "Busan").AsTask(), count: 2);

        Assert.Equal(["Seoul", "Incheon"], collected[1]);
    }

    [Fact]
    public async Task ListState_RemoveAsync_ByItem_RemovesItem()
    {
        var state = ListState.Value(["Seoul", "Busan", "Incheon"]);
        var collected = await CollectAsync(state, mutate: () => state.RemoveAsync("Busan").AsTask(), count: 2);

        Assert.Equal(["Seoul", "Incheon"], collected[1]);
    }

    [Fact]
    public async Task ListState_UpdateAsync_OldNewItem_ReplacesItem()
    {
        var state = ListState.Value(["Seoul", "Busan"]);
        var collected = await CollectAsync(state, mutate: () => state.UpdateAsync("Busan", "Incheon").AsTask(), count: 2);

        Assert.Equal(["Seoul", "Incheon"], collected[1]);
    }

    [Fact]
    public async Task ListState_UpdateAsync_Predicate_UpdatesMatchingItem()
    {
        var state = ListState.Value(["Seoul", "busan"]);
        var collected = await CollectAsync(state,
            mutate: () => state.UpdateAsync(x => x == "busan", x => x.ToUpper()).AsTask(), count: 2);

        Assert.Equal(["Seoul", "BUSAN"], collected[1]);
    }

    [Fact]
    public async Task ListState_SetAsync_ReplacesEntireList()
    {
        var state = ListState.Value(["Seoul", "Busan"]);
        var collected = await CollectAsync(state, mutate: () => state.SetAsync(["Incheon", "Daegu"]).AsTask(), count: 2);

        Assert.Equal(["Incheon", "Daegu"], collected[1]);
    }

    [Fact]
    public async Task ListState_ClearAsync_EmptiesList()
    {
        var state = ListState.Value(["Seoul", "Busan"]);
        var collected = await CollectAsync(state, mutate: () => state.ClearAsync().AsTask(), count: 2);

        Assert.Empty(collected[1]);
    }

    [Fact]
    public async Task ListState_ForEachAsync_ReceivesEachListVersion()
    {
        var state = ListState.Value(["Seoul"]);
        var received = new List<IReadOnlyList<string>>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var loop = state.ForEachAsync((list, _) => { received.Add(list.ToList()); return ValueTask.CompletedTask; }, cts.Token);

        await Task.Delay(20);
        await state.AddAsync("Busan");
        await Task.Delay(20);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loop.AsTask());
        Assert.Equal(2, received.Count);
        Assert.Equal(["Seoul"], received[0]);
        Assert.Equal(["Seoul", "Busan"], received[1]);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task<List<List<string>>> CollectAsync(
        IListState<string> state, Func<Task> mutate, int count)
    {
        var collected = new List<List<string>>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var reader = Task.Run(async () =>
        {
            await foreach (var msg in state.GetSource(cts.Token))
            {
                if (msg.Data.IsSome(out var list))
                    collected.Add(list.ToList());
                if (collected.Count >= count) break;
            }
        });

        await Task.Delay(20);
        await mutate();
        await reader;
        return collected;
    }

    private static async Task<Message<IReadOnlyList<T>>> FirstAsync<T>(IListFeed<T> feed)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var msg in feed.GetSource(cts.Token))
            return msg;
        throw new InvalidOperationException("Feed produced no messages.");
    }
}

using Lw.Mvux;

namespace Mvux.Core.Tests;

public class FeedExtensionsTests
{
    // ── UpdateAsync(Func<T?, T?>) ────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_SimpleFunc_TransformsValue()
    {
        var state = State.Value("seoul");

        await state.UpdateAsync(v => v?.ToUpper());

        var value = await state;
        Assert.Equal("SEOUL", value);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_SetsNone()
    {
        var state = State.Value("seoul");

        await state.UpdateAsync(_ => (string?)null);

        var msg = await FirstMessageAsync(state);
        Assert.True(msg.IsNone);
    }

    [Fact]
    public async Task UpdateAsync_EmptyState_ReceivesDefaultInput()
    {
        var state = State.Empty<string>();
        string? received = "not-called";

        await state.UpdateAsync(v => { received = v; return "result"; });

        Assert.Null(received);
        Assert.Equal("result", await state);
    }

    // ── ForEachAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ForEachAsync_ReceivesEachDataValue()
    {
        var state = State.Value(1);
        var collected = new List<int>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var loop = state.ForEachAsync(v => { collected.Add(v); return ValueTask.CompletedTask; }, cts.Token);

        await Task.Delay(20);
        await state.SetAsync(2);
        await state.SetAsync(3);
        await Task.Delay(20);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loop.AsTask());
        Assert.Equal([1, 2, 3], collected);
    }

    [Fact]
    public async Task ForEachAsync_SkipsLoadingAndError()
    {
        var emitted = new List<string>();
        var feed = Feed.Async<string>((Func<CancellationToken, Task<string>>)(async ct =>
        {
            await Task.Delay(10, ct);
            return "data";
        }));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await feed.ForEachAsync(v => { emitted.Add(v); return ValueTask.CompletedTask; }, cts.Token);

        Assert.Equal(["data"], emitted);
    }

    [Fact]
    public async Task ForEachAsync_ActionOverload_Works()
    {
        var state = State.Value(42);
        var received = new List<int>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var loop = state.ForEachAsync(v => received.Add(v), cts.Token);

        await Task.Delay(20);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loop.AsTask());

        Assert.Contains(42, received);
    }

    // ── GetAwaiter (await state) ─────────────────────────────────────────────

    [Fact]
    public async Task Await_State_ReturnsCurrentValue()
    {
        var state = State.Value("Seoul");

        var value = await state;

        Assert.Equal("Seoul", value);
    }

    [Fact]
    public async Task Await_EmptyState_ReturnsDefault()
    {
        var state = State.Empty<string>();

        var value = await state;

        Assert.Null(value);
    }

    [Fact]
    public async Task Await_State_ReturnsLatestAfterSet()
    {
        var state = State.Value("Seoul");
        await state.SetAsync("Busan");

        var value = await state;

        Assert.Equal("Busan", value);
    }

    // ── Select ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Select_TransformsDataValue()
    {
        var state = State.Value(5);

        var feed = state.Select(x => x * 2);
        var msg = await FirstMessageAsync(feed);

        Assert.True(msg.HasData);
        Assert.Equal(10, msg.Data.Value);
    }

    [Fact]
    public async Task Select_PropagatesNone()
    {
        var state = State.Empty<int>();

        var feed = state.Select(x => x * 2);
        var msg = await FirstMessageAsync(feed);

        Assert.True(msg.IsNone);
    }

    // ── SelectAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SelectAsync_TransformsDataWithLoading()
    {
        var state = State.Value("Seoul");
        var messages = new List<Message<string>>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        Func<string, CancellationToken, Task<string>> selector =
            async (city, ct) => { await Task.Delay(20, ct); return city.ToUpper(); };
        var feed = state.SelectAsync(selector);

        var loop = Task.Run(async () =>
        {
            await foreach (var msg in feed.GetSource(cts.Token))
            {
                messages.Add(msg);
                if (msg.HasData) break;
            }
        });

        await loop;

        Assert.Contains(messages, m => m.IsLoading);
        Assert.Contains(messages, m => m.HasData && m.Data.Value == "SEOUL");
    }

    [Fact]
    public async Task SelectAsync_Task_Overload_Works()
    {
        var state = State.Value(3);

        var feed = state.SelectAsync(x => Task.FromResult(x + 10));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        string? result = null;
        await foreach (var msg in feed.GetSource(cts.Token))
        {
            if (msg.HasData) { result = msg.Data.Value.ToString(); break; }
        }

        Assert.Equal("13", result);
    }

    [Fact]
    public async Task SelectAsync_SelectorThrows_ProducesError()
    {
        var state = State.Value("bad");
        var ex = new InvalidOperationException("oops");

        Func<string, CancellationToken, Task<string>> throws = (_, _) => throw ex;
        var feed = state.SelectAsync(throws);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Message<string>? errorMsg = null;
        await foreach (var msg in feed.GetSource(cts.Token))
        {
            if (msg.Error != null) { errorMsg = msg; break; }
            if (msg.HasData) break;
        }

        Assert.NotNull(errorMsg);
        Assert.Same(ex, errorMsg!.Value.Error);
    }

    // ── Where ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Where_MatchingPredicate_PassesThrough()
    {
        var state = State.Value(10);

        var feed = state.Where(x => x > 5);
        var msg = await FirstMessageAsync(feed);

        Assert.True(msg.HasData);
        Assert.Equal(10, msg.Data.Value);
    }

    [Fact]
    public async Task Where_NonMatchingPredicate_YieldsNone()
    {
        var state = State.Value(3);

        var feed = state.Where(x => x > 5);
        var msg = await FirstMessageAsync(feed);

        Assert.True(msg.IsNone);
    }

    // ── SetNoneAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SetNoneAsync_ClearsValue()
    {
        var state = State.Value("hello");

        await state.SetNoneAsync();

        var msg = await FirstMessageAsync(state);
        Assert.True(msg.IsNone);
    }

    // ── UpdateAsync (async overload) ─────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_AsyncOverload_TransformsValue()
    {
        var state = State.Value("hello");

        await state.UpdateAsync(async (v, ct) =>
        {
            await Task.Delay(5, ct);
            return v?.ToUpper();
        });

        Assert.Equal("HELLO", await state);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static async Task<Message<T>> FirstMessageAsync<T>(IFeed<T> feed)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var msg in feed.GetSource(cts.Token))
            return msg;
        throw new InvalidOperationException("Feed produced no messages.");
    }
}

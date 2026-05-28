using Luke.Mvux;

namespace Mvux.Core.Tests;

public class StateTests
{
    private sealed class Owner
    {
        public IState<string> City => State.Value(this, () => "Seoul");
        public IState<string> Country => State.Value(this, () => "Korea");
    }

    [Fact]
    public void State_Value_WithOwner_ReturnsSameInstance_PerProperty()
    {
        var owner = new Owner();

        var first = owner.City;
        var second = owner.City;

        Assert.Same(first, second);
    }

    [Fact]
    public async Task State_Value_WithOwner_TypeInference_UsesDistinctKeys_PerProperty()
    {
        var owner = new Owner();

        var city = await owner.City;
        var country = await owner.Country;

        Assert.Equal("Seoul", city);
        Assert.Equal("Korea", country);
        Assert.NotSame(owner.City, owner.Country);
    }

    [Fact]
    public async Task State_Value_ReplaysCurrent()
    {
        var state = State.Value("Seoul");

        var msg = await FirstAsync(state);

        Assert.Equal("Seoul", msg.Data.Value);
    }

    [Fact]
    public async Task State_Empty_ReturnsNone()
    {
        var state = State.Empty<string>();

        var msg = await FirstAsync(state);

        Assert.True(msg.IsNone);
    }

    [Fact]
    public async Task State_SetAsync_BroadcastsNewValue()
    {
        var state = State.Value("Seoul");
        var collected = new List<Message<string>>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var reader = Task.Run(async () =>
        {
            await foreach (var msg in state.GetSource(cts.Token))
            {
                collected.Add(msg);
                if (collected.Count >= 2) break;
            }
        });

        await Task.Delay(20);
        await state.SetAsync("Busan");
        await reader;

        Assert.Equal("Seoul", collected[0].Data.Value);
        Assert.Equal("Busan", collected[1].Data.Value);
    }

    [Fact]
    public async Task State_MultipleSubscribers_AllReceiveUpdate()
    {
        var state = State.Value(0);
        var results = new List<int>[3];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var tasks = Enumerable.Range(0, 3).Select(i =>
        {
            results[i] = [];
            return Task.Run(async () =>
            {
                await foreach (var msg in state.GetSource(cts.Token))
                {
                    if (msg.Data.IsSome(out var v))
                        results[i].Add(v);
                    if (results[i].Count >= 2) break;
                }
            });
        }).ToArray();

        await Task.Delay(30);
        await state.SetAsync(99);
        await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            Assert.Equal(0, result[0]);
            Assert.Equal(99, result[1]);
        }
    }

    [Fact]
    public async Task State_Async_MultipleSubscribers_ShareSingleFetch()
    {
        var calls = 0;
        var gate = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<CancellationToken, Task<string>> fetch = async ct =>
        {
            Interlocked.Increment(ref calls);
            using var registration = ct.Register(() => gate.TrySetCanceled(ct));
            return await gate.Task;
        };
        var state = State.Async(fetch);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var first = ReadFirstDataAsync(state, cts.Token);
        var second = ReadFirstDataAsync(state, cts.Token);

        await Task.Delay(50, cts.Token);
        gate.TrySetResult("Seoul");

        var values = await Task.WhenAll(first, second);

        Assert.Equal(1, Volatile.Read(ref calls));
        Assert.Equal(new[] { "Seoul", "Seoul" }, values);
    }

    [Fact]
    public async Task State_Async_ReplaysLoadedValue_ToNewSubscriber()
    {
        Func<CancellationToken, Task<string>> fetch = async ct =>
        {
            await Task.Delay(10, ct);
            return "Seoul";
        };
        var state = State.Async(fetch);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var first = await ReadFirstDataAsync(state, cts.Token);
        var second = await ReadFirstDataAsync(state, cts.Token);

        Assert.Equal("Seoul", first);
        Assert.Equal("Seoul", second);
    }

    private static async Task<Message<T>> FirstAsync<T>(IFeed<T> feed)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var msg in feed.GetSource(cts.Token))
            return msg;
        throw new InvalidOperationException("Feed produced no messages.");
    }

    private static async Task<T> ReadFirstDataAsync<T>(IFeed<T> feed, CancellationToken ct)
    {
        await foreach (var msg in feed.GetSource(ct))
            if (msg.Data.IsSome(out var value))
                return value;

        throw new InvalidOperationException("Feed produced no data.");
    }
}

using Luke.Mvux;

namespace Mvux.Core.Tests;

public class StateTests
{
    private sealed class Owner
    {
        public IState<string> City => State.Value(this, () => "Seoul");
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

    private static async Task<Message<T>> FirstAsync<T>(IFeed<T> feed)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var msg in feed.GetSource(cts.Token))
            return msg;
        throw new InvalidOperationException("Feed produced no messages.");
    }
}

using Mvux.Wpf.Core;

namespace Mvux.Wpf.Core.Tests;

public class SignalTests
{
    [Fact]
    public async Task Signal_Raise_NotifiesListeners()
    {
        var signal = new Signal();
        var received = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var listener = Task.Run(async () =>
        {
            await foreach (var _ in signal.Listen(cts.Token))
            {
                received++;
                if (received >= 2) break;
            }
        });

        await Task.Delay(20);
        await signal.Raise();
        await signal.Raise();
        await listener;

        Assert.Equal(2, received);
    }

    [Fact]
    public async Task Signal_MultipleListeners_AllNotified()
    {
        var signal = new Signal();
        var counts = new int[3];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var listeners = Enumerable.Range(0, 3).Select(i =>
            Task.Run(async () =>
            {
                await foreach (var _ in signal.Listen(cts.Token))
                {
                    counts[i]++;
                    break;
                }
            })
        ).ToArray();

        await Task.Delay(30);
        await signal.Raise();
        await Task.WhenAll(listeners);

        Assert.All(counts, c => Assert.Equal(1, c));
    }
}

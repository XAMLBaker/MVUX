using Luke.Mvux;

namespace Mvux.Core.Tests;

public class FeedTests
{
    [Fact]
    public async Task AsyncFeed_EmitsLoadingThenData()
    {
        Func<CancellationToken, Task<string>> fetch = async ct =>
        {
            await Task.Delay(10, ct);
            return "hello";
        };
        var feed = Feed.Async(fetch);

        var messages = await CollectAsync(feed, count: 2);

        Assert.True(messages[0].IsLoading);
        Assert.Equal("hello", messages[1].Data.Value);
    }

    [Fact]
    public async Task AsyncFeed_EmitsLoadingThenError()
    {
        Func<CancellationToken, Task<string>> fetch = async ct =>
        {
            await Task.Delay(10, ct);
            throw new InvalidOperationException("fail");
        };
        var feed = Feed.Async(fetch);

        var messages = await CollectAsync(feed, count: 2);

        Assert.True(messages[0].IsLoading);
        Assert.IsType<InvalidOperationException>(messages[1].Error);
    }

    private static async Task<List<Message<T>>> CollectAsync<T>(IFeed<T> feed, int count)
    {
        var result = new List<Message<T>>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await foreach (var msg in feed.GetSource(cts.Token))
        {
            result.Add(msg);
            if (result.Count >= count) break;
        }

        return result;
    }
}

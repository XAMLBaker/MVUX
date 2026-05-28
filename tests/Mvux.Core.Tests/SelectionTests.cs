using Luke.Mvux;
using System.Collections.Immutable;

namespace Mvux.Core.Tests;

public class SelectionTests
{
    [Fact]
    public async Task Selection_SetSelectedAsync_With_Wrong_Type_Clears_State()
    {
        var list = ListState.Value(new List<string> { "Seoul" });
        var selected = State.Value("Seoul");
        var withSelection = list.Selection(selected);

        var selectionFeed = Assert.IsAssignableFrom<ISelectionFeed>(withSelection);
        await selectionFeed.SetSelectedAsync(123);

        var selectedNow = await selected;
        Assert.Null(selectedNow);
    }

    [Fact]
    public async Task Selection_GetSelectionMessages_Reflects_Updated_Selected_Item()
    {
        var list = ListState.Value(new List<string> { "Seoul", "Busan" });
        var selected = State.Value("Seoul");
        var withSelection = list.Selection(selected);

        var selectionFeed = Assert.IsAssignableFrom<ISelectionFeed>(withSelection);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await selectionFeed.SetSelectedAsync("Busan", cts.Token);
        var msg = await FirstMessageAsync(selectionFeed.GetSelectionMessages(cts.Token), cts.Token);

        Assert.True(msg.HasData);
        Assert.Equal("Busan", msg.DataObject);
    }

    [Fact]
    public async Task MultiSelection_SetSelectedAsync_Updates_Selected_Items_State()
    {
        var list = ListState.Value(new List<string> { "Seoul", "Busan", "Incheon" });
        var selectedItems = State.Value(ImmutableList<string>.Empty);
        var withSelection = list.Selection(selectedItems);

        var selectionFeed = Assert.IsAssignableFrom<ISelectionFeed>(withSelection);
        Assert.True(selectionFeed.SupportsMultipleSelection);

        await selectionFeed.SetSelectedAsync(new[] { "Seoul", "Incheon" });

        var current = await selectedItems;
        Assert.Equal(["Seoul", "Incheon"], current);
    }

    [Fact]
    public async Task MultiSelection_Prunes_Removed_Items()
    {
        var list = ListState.Value(new List<string> { "Seoul", "Busan", "Incheon" });
        var selectedItems = State.Value(ImmutableList.Create("Seoul", "Incheon"));
        var withSelection = list.Selection(selectedItems);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var subscription = Task.Run(async () =>
        {
            try
            {
                await foreach (var _ in withSelection.GetSource(cts.Token))
                {
                }
            }
            catch (OperationCanceledException)
            {
            }
        });

        await list.RemoveAsync("Incheon", cts.Token);
        await WaitForAsync(async () =>
        {
            var current = await selectedItems;
            return current is not null && current.SequenceEqual(["Seoul"]);
        }, cts.Token);

        cts.Cancel();
        await subscription;

        var current = await selectedItems;
        Assert.Equal(["Seoul"], current);
    }

    private static async Task<IMessage> FirstMessageAsync(IAsyncEnumerable<IMessage> source, CancellationToken ct)
    {
        await foreach (var msg in source.WithCancellation(ct))
            return msg;

        throw new InvalidOperationException("Selection feed produced no message.");
    }

    private static async Task WaitForAsync(Func<Task<bool>> condition, CancellationToken ct)
    {
        while (!await condition())
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(10, ct);
        }
    }

}

using Luke.Mvux;

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

    private static async Task<IMessage> FirstMessageAsync(IAsyncEnumerable<IMessage> source, CancellationToken ct)
    {
        await foreach (var msg in source.WithCancellation(ct))
            return msg;

        throw new InvalidOperationException("Selection feed produced no message.");
    }

}

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Luke.Mvux;

namespace Avalonia.MultiSelection.Sample;

public enum WorkLane
{
    Ready,
    Investigating,
    Blocked
}

public record WorkCard(string Id, string Title, string Owner, WorkLane Lane, bool Escalated);

public partial record MultiSelectionModel()
{
    public IState<string> DraftTitle => State.Value(this, () => "Investigate checkout retries");

    public IListState<WorkCard> Queue => ListState.Value(this, () => new List<WorkCard>
    {
        new("OPS-2410", "Payment provider timeout wave", "Mina", WorkLane.Investigating, true),
        new("OPS-2411", "Search index lag above SLA", "Joon", WorkLane.Ready, false),
        new("OPS-2412", "Warehouse sync blocked by retry storm", "Ari", WorkLane.Blocked, true),
        new("OPS-2413", "Catalog cache drift in eu-west", "Theo", WorkLane.Ready, false),
        new("OPS-2414", "Checkout fraud queue backlog", "Lena", WorkLane.Investigating, true)
    });

    public IState<ImmutableList<WorkCard>> SelectedCards => State.Value(this, () => ImmutableList<WorkCard>.Empty);

    public IListFeed<WorkCard> QueueWithSelection => Queue.Selection(SelectedCards);

    public IListFeed<WorkCard> SelectedCardItems => ListFeed.AsyncEnumerable(GetSelectedCardItems);

    public async ValueTask AddCard(CancellationToken ct)
    {
        var draft = await DraftTitle;
        if (string.IsNullOrWhiteSpace(draft))
            return;

        var card = new WorkCard(
            $"OPS-{Random.Shared.Next(2500, 2999)}",
            draft.Trim(),
            "Rotation",
            WorkLane.Ready,
            false);

        await Queue.InsertAtAsync(0, card, ct);
        await SelectedCards.SetAsync(ImmutableList.Create(card), ct);
    }

    public async ValueTask SelectRecommended(CancellationToken ct)
    {
        var current = await GetCurrentQueueAsync(ct);
        var recommended = current
            .Where(card => card.Escalated || card.Lane == WorkLane.Blocked)
            .Take(3)
            .ToImmutableList();

        await SelectedCards.SetAsync(recommended, ct);
    }

    public async ValueTask ClearSelection(CancellationToken ct)
        => await SelectedCards.SetAsync(ImmutableList<WorkCard>.Empty, ct);

    public async ValueTask PromoteSelected(CancellationToken ct)
    {
        var selected = await SelectedCards;
        if (selected is null || selected.Count == 0)
            return;

        var selectedIds = selected.Select(card => card.Id).ToHashSet(StringComparer.Ordinal);
        await Queue.UpdateAllAsync(
            card => selectedIds.Contains(card.Id),
            card => card with { Lane = WorkLane.Investigating, Escalated = true },
            ct);
    }

    public async ValueTask CompleteSelected(CancellationToken ct)
    {
        var selected = await SelectedCards;
        if (selected is null || selected.Count == 0)
            return;

        var selectedIds = selected.Select(card => card.Id).ToHashSet(StringComparer.Ordinal);
        await Queue.RemoveAsync(card => selectedIds.Contains(card.Id), ct);
        await SelectedCards.SetAsync(ImmutableList<WorkCard>.Empty, ct);
    }

    private async ValueTask<IReadOnlyList<WorkCard>> GetCurrentQueueAsync(CancellationToken ct)
    {
        await foreach (var message in Queue.GetSource(ct))
            if (message.Data.IsSome(out var items))
                return items;

        return [];
    }

    private async IAsyncEnumerable<IReadOnlyList<WorkCard>> GetSelectedCardItems(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var message in SelectedCards.GetSource(ct))
            yield return message.Data.IsSome(out var items) ? items : ImmutableList<WorkCard>.Empty;
    }
}

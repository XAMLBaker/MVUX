using Luke.Mvux;

namespace Wpf.Demo.Sample;

public enum WorkPriority
{
    Low,
    Medium,
    High,
    Critical
}

public record WorkItem(string Id, string Title, WorkPriority Priority, string Team, DateTime CreatedAt);

public record WorkItemInsight(string Id, string Summary, string Recommendation, int RiskScore, bool Escalate);

public interface IOperationsService
{
    Task<WorkItemInsight> GetInsightAsync(WorkItem item, CancellationToken ct);
}

public class FakeOperationsService : IOperationsService
{
    public async Task<WorkItemInsight> GetInsightAsync(WorkItem item, CancellationToken ct)
    {
        await Task.Delay(1000, ct);

        if (item.Title.Contains("FAIL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Simulated backend failure for selected work item.");

        var risk = item.Priority switch
        {
            WorkPriority.Critical => 92,
            WorkPriority.High => 78,
            WorkPriority.Medium => 52,
            _ => 25
        };

        var recommendation = risk >= 80
            ? "Escalate to on-call lead and create hotfix branch."
            : risk >= 60
                ? "Assign owner and verify rollback readiness."
                : "Track in normal queue and monitor for drift.";

        return new WorkItemInsight(
            item.Id,
            $"{item.Team} queue item '{item.Title}' analyzed.",
            recommendation,
            risk,
            risk >= 80);
    }
}

public partial record OperationsModel(IOperationsService OperationsService)
{
    public IState<string> DraftTitle => State.Value(this, () => "API latency spike");

    public IListState<WorkItem> Queue => ListState.Value(this, () => new List<WorkItem>
    {
        new("OPS-1021", "Payment timeout alerts", WorkPriority.High, "Payments", DateTime.UtcNow.AddMinutes(-20)),
        new("OPS-1022", "Order projection lag", WorkPriority.Medium, "Catalog", DateTime.UtcNow.AddMinutes(-13)),
        new("OPS-1023", "FAIL: Inventory sync anomaly", WorkPriority.Critical, "Inventory", DateTime.UtcNow.AddMinutes(-5))
    });

    public IState<WorkItem> SelectedWorkItem => State<WorkItem>.Empty(this);

    public IListFeed<WorkItem> QueueWithSelection => Queue.Selection(SelectedWorkItem);

    public IFeed<WorkItemInsight> SelectedInsight =>
        SelectedWorkItem.SelectAsync((item, ct) => OperationsService.GetInsightAsync(item, ct));

    public async ValueTask AddWorkItem(CancellationToken ct)
    {
        var title = await DraftTitle;
        if (string.IsNullOrWhiteSpace(title))
            return;

        var newItem = new WorkItem(
            $"OPS-{Random.Shared.Next(2000, 2999)}",
            title.Trim(),
            WorkPriority.Medium,
            "Core Platform",
            DateTime.UtcNow);

        await Queue.InsertAtAsync(0, newItem, ct);
        await SelectedWorkItem.SetAsync(newItem, ct);
    }

    public async ValueTask ResolveSelected(CancellationToken ct)
    {
        var selected = await SelectedWorkItem;
        if (selected is null)
            return;

        await Queue.RemoveAsync(selected, ct);
        await SelectedWorkItem.SetNoneAsync(ct);
    }

    public async ValueTask SimulateFailure(CancellationToken ct)
    {
        var failing = new WorkItem(
            $"OPS-{Random.Shared.Next(3000, 3999)}",
            "FAIL: downstream 500 burst",
            WorkPriority.Critical,
            "Gateway",
            DateTime.UtcNow);

        await Queue.InsertAtAsync(0, failing, ct);
        await SelectedWorkItem.SetAsync(failing, ct);
    }
}

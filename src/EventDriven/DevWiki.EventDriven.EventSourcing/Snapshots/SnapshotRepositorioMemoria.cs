namespace DevWiki.EventDriven.EventSourcing.Snapshots;

public sealed class SnapshotRepositorioMemoria : ISnapshotRepositorio
{
    private readonly Dictionary<Guid, Snapshot> _snapshots = [];

    public Task SalvarAsync(Snapshot snapshot, CancellationToken ct = default)
    {
        _snapshots[snapshot.AggregateId] = snapshot;
        return Task.CompletedTask;
    }

    public Task<Snapshot?> ObterUltimoAsync(Guid aggregateId, CancellationToken ct = default)
    {
        _snapshots.TryGetValue(aggregateId, out var snapshot);
        return Task.FromResult(snapshot);
    }
}
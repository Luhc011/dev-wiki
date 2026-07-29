namespace DevWiki.EventDriven.EventSourcing.Snapshots;

public interface ISnapshotRepositorio
{
    Task SalvarAsync(Snapshot snapshot, CancellationToken ct = default);
    Task<Snapshot?> ObterUltimoAsync(Guid aggregateId, CancellationToken ct = default);
}
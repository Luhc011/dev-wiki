using DevWiki.EventDriven.EventSourcing.Aggregates;
using DevWiki.EventDriven.EventSourcing.EventStore;
using DevWiki.EventDriven.EventSourcing.Snapshots;

namespace DevWiki.EventDriven.EventSourcing.Repositories;

public sealed class RepositorioCobrancaEventSourcing(IEventStore eventStore, ISnapshotRepositorio snapshotRepositorio)
{
    public int IntervaloSnapshot
    {
        get;
        set => field = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "IntervaloSnapshot deve ser positivo");
    } = 10;

    public async Task<CobrancaAggregate?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var snapshot = await snapshotRepositorio.ObterUltimoAsync(id, ct);

        if (snapshot is not null)
        {
            var eventosPosSnapshot = await eventStore.CarregarAPartirDeAsync(id, snapshot.Versao, ct);
            if (eventosPosSnapshot.Count == 0 && snapshot.Versao == 0) return null;

            return CobrancaAggregate.ReconstituirDeSnapshot(snapshot, eventosPosSnapshot);
        }

        var todosEventos = await eventStore.CarregarAsync(id, ct);
        if (todosEventos.Count == 0) return null;

        return CobrancaAggregate.Reconstituir(todosEventos);
    }

    public async Task SalvarAsync(CobrancaAggregate aggregate, long versaoEsperada, CancellationToken ct = default)
    {
        var pendentes = aggregate.EventosPendentes;
        if (pendentes.Count == 0) return;

        await eventStore.SalvarAsync(pendentes, versaoEsperada, ct);

        if (aggregate.Versao % IntervaloSnapshot == 0)
        {
            var snapshot = new Snapshot(aggregate.Id,
                                        aggregate.Versao,
                                        aggregate.SerializarEstado(),
                                        DateTimeOffset.UtcNow);

            await snapshotRepositorio.SalvarAsync(snapshot, ct);
        }
    }
}
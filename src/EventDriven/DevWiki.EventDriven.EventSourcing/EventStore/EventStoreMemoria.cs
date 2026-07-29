using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EventSourcing.EventStore;

public interface IEventStore
{
    Task<IReadOnlyList<AggregateEvent>> CarregarAsync(Guid aggregateId, CancellationToken ct = default);
    Task<IReadOnlyList<AggregateEvent>> CarregarAPartirDeAsync(Guid aggregateId,
                                                               long versaoInicial,
                                                               CancellationToken ct = default);
    Task SalvarAsync(IReadOnlyList<AggregateEvent> eventos,
                     long versaoEsperada,
                     CancellationToken ct = default);
}

public sealed class EventStoreMemoria : IEventStore
{
    private readonly Dictionary<Guid, List<AggregateEvent>> _store = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<AggregateEvent>> CarregarAsync(Guid aggregateId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_store.TryGetValue(aggregateId, out var eventos))
                return Task.FromResult<IReadOnlyList<AggregateEvent>>([]);

            return Task.FromResult<IReadOnlyList<AggregateEvent>>([.. eventos]);
        }
    }

    public Task<IReadOnlyList<AggregateEvent>> CarregarAPartirDeAsync(Guid aggregateId, long versaoInicial, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_store.TryGetValue(aggregateId, out var todos))
                return Task.FromResult<IReadOnlyList<AggregateEvent>>([]);

            var filtrados = todos.Where(e => e.Versao > versaoInicial).ToList();
            return Task.FromResult<IReadOnlyList<AggregateEvent>>([.. filtrados]);
        }
    }

    public Task SalvarAsync(IReadOnlyList<AggregateEvent> eventos, long versaoEsperada, CancellationToken ct = default)
    {
        if (eventos.Count == 0) return Task.CompletedTask;

        lock (_lock)
        {
            var aggragateId = eventos[0].AggregateId;

            if (!_store.TryGetValue(aggragateId, out var existentes))
            {
                existentes = [];
                _store[aggragateId] = existentes;
            }

            var versaoAtual = existentes.Count > 0 ? existentes[^1].Versao : 0L;

            if (versaoAtual != versaoEsperada)
                throw new ConcorrenciaException(aggragateId, versaoEsperada, versaoAtual);

            existentes.AddRange(eventos);
        }

        return Task.CompletedTask;
    }
}
namespace DevWiki.Microservices.Outbox.Interfaces;

public interface IOutboxRepositorio
{
    Task SalvarEventoPendenteAsync(OutboxEvento evento, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxEvento>> BuscarPendentesAsync(int limite = 100, CancellationToken ct = default);
    Task MarcarComoProcessadoAsync(Guid eventoId, CancellationToken ct = default);
    Task IncrementarTentativaAsync(Guid eventoId, CancellationToken ct = default);
}

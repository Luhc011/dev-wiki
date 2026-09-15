using DevWiki.Microservices.Outbox.Interfaces;

namespace DevWiki.Microservices.Outbox.Repositories;

public sealed class OutboxRepositorioMemoria : IOutboxRepositorio
{
    private readonly List<OutboxEvento> _eventos = [];

    public int CapacidadeMaxima
    {
        get;
        init => field = value is > 0 and <= 1_000_000
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value));
    } = 100_000;

    public Task SalvarEventoPendenteAsync(OutboxEvento evento, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evento, nameof(evento));
        _eventos.Add(evento);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxEvento>> BuscarPendentesAsync(int limite = 100, CancellationToken ct = default)
    {
        IReadOnlyList<OutboxEvento> resultado = [..
            _eventos
                .Where(e => !e.Processado)
                .OrderBy(e => e.CriadoEm)
                .Take(limite)];
        return Task.FromResult(resultado);
    }

    public Task MarcarComoProcessadoAsync(Guid eventoId, CancellationToken ct = default)
    {
        var evento = _eventos.FirstOrDefault(e => e.Id == eventoId);
        if (evento is not null)
            evento.ProcessadoEm = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public Task IncrementarTentativaAsync(Guid eventoId, CancellationToken ct = default)
    {
        var evento = _eventos.FirstOrDefault(e => e.Id == eventoId);
        if (evento is not null)
            evento.TentativasProcessamento++;
        return Task.CompletedTask;
    }

    public int Total => _eventos.Count;
    public int TotalProcessados => _eventos.Count(e => e.Processado);
    public int TotalPendentes => _eventos.Count(e => !e.Processado);
}
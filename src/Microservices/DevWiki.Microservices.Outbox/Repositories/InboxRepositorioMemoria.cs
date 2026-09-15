using DevWiki.Microservices.Outbox.Interfaces;

namespace DevWiki.Microservices.Outbox.Repositories;

public sealed class InboxRepositorioMemoria : IInboxRepositorio
{
    private readonly HashSet<Guid> _mensagensProcessadas = [];

    public Task<bool> JaProcessadoAsync(Guid mensagemId, CancellationToken ct = default) =>
        Task.FromResult(_mensagensProcessadas.Contains(mensagemId));

    public Task RegistrarAsync(InboxEvento evento, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evento, nameof(evento));
        _mensagensProcessadas.Add(evento.MensagemId);
        return Task.CompletedTask;
    }

    public int Total => _mensagensProcessadas.Count;
}
namespace DevWiki.Microservices.Outbox.Interfaces;

public interface IInboxRepositorio
{
    Task<bool> JaProcessadoAsync(Guid mensagemId, CancellationToken ct = default);

    Task RegistrarAsync(InboxEvento evento, CancellationToken ct = default);
}
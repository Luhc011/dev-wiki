namespace DevWiki.Microservices.Outbox.Interfaces;

public interface IPublicadorMensagens
{
    Task<bool> PublicarAsync(OutboxEvento evento, CancellationToken ct = default);
}
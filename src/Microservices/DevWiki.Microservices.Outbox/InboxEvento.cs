namespace DevWiki.Microservices.Outbox;

public sealed class InboxEvento
{
    public required Guid MensagemId { get; init; }
    public required string NomeEvento { get; init; }
    public DateTimeOffset RecebidoEm { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ProcessadoEm { get; } = DateTimeOffset.UtcNow;
}
namespace DevWiki.EventDriven.Messaging.RabbitMQ;

public sealed class RabbitMQMensagem
{
    public Guid MensagemId { get; } = Guid.NewGuid();
    public required string RoutingKey { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    public int TentativasEntrega
    {
        get;
        set => field = value >= field
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "TentativasEntrega só pode aumentar.");
    }

    public void IncrementarTentativa() => TentativasEntrega++;
}
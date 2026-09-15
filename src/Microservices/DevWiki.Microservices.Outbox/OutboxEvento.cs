namespace DevWiki.Microservices.Outbox;

public sealed class OutboxEvento
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string NomeEvento { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset CriadoEm { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessadoEm { get; set; }

    public bool Processado => ProcessadoEm is not null;

    public int TentativasProcessamento
    {
        get;
        set => field = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Tentativas n podem ser negativas.");
    }
}
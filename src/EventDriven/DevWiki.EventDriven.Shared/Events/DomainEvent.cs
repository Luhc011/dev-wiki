namespace DevWiki.EventDriven.Shared.Events;

public abstract record DomainEvent
{
    public Guid EventoId { get; } = Guid.NewGuid();
    public DateTimeOffset OcorridoEm { get; } = DateTimeOffset.UtcNow;
    public abstract string NomeEvento { get; }
}
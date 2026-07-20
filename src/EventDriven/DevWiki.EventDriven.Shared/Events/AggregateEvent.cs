namespace DevWiki.EventDriven.Shared.Events;

public abstract record AggregateEvent : DomainEvent
{
    public required Guid AggregateId { get; init; }
    public required long Versao { get; init; }
}

using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EventSourcing.Events;

public sealed record CobrancaVencidaEvent : AggregateEvent
{
    public required DateTimeOffset VencidaEm { get; init; }

    public override string NomeEvento => nameof(CobrancaVencidaEvent);
}

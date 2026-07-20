using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EventSourcing.Events;

public sealed record CobrancaCriadaEvent : AggregateEvent
{
    public required string CpfDevedor { get; init; }
    public required decimal Valor { get; init; }
    public required DateTimeOffset DataVencimento { get; init; }
    public override string NomeEvento => nameof(CobrancaCriadaEvent);
}

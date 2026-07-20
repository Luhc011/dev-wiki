using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EventSourcing.Events;

public sealed record PagamentoRegistradoEvent : AggregateEvent
{
    public required decimal ValorPago { get; init; }
    public required DateTimeOffset PagoEm { get; init; }

    public override string NomeEvento => nameof(PagamentoRegistradoEvent);
}
using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EDA.Events;

public sealed record class PagamentoProcessadoEvento : DomainEvent
{
    public required Guid CobrancaId { get; init; }
    public required string CpfPagador { get; init; }
    public required decimal Valor { get; init; }
    public required string TipoPagamento { get; init; }

    public override string NomeEvento => nameof(PagamentoProcessadoEvento);
}
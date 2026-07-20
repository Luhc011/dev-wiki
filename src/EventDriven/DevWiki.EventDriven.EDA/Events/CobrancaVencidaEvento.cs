using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EDA.Events;

public sealed record CobrancaVencidaEvento : DomainEvent
{
    public required Guid CobrancaId { get; init; }
    public required string CpfDevedor { get; init; }
    public required decimal ValorOriginal { get; init; }
    public required DateTimeOffset DataVencimento { get; init; }

    public override string NomeEvento => nameof(CobrancaVencidaEvento);
}
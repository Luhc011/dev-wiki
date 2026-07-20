using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EDA.Events;

public sealed record FraudeDetectadaEvento : DomainEvent
{
    public required Guid CobrancaId { get; init; }
    public required string CpfSuspeito { get; init; }
    public required string MotivoDaSuspeitia { get; init; }
    public required decimal ValorSuspeito { get; init; }

    public override string NomeEvento => nameof(FraudeDetectadaEvento);
}

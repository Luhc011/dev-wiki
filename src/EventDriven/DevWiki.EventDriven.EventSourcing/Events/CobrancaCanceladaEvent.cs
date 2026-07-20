using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EventSourcing.Events;

public sealed record CobrancaCanceladaEvent : AggregateEvent
{
    public required string Motivo { get; init; }
    public required DateTimeOffset CanceladoEm { get; init; }
    public override string NomeEvento => nameof(CobrancaCanceladaEvent);
}
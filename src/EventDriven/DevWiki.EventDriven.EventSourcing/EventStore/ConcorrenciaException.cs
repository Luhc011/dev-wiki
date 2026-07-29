namespace DevWiki.EventDriven.EventSourcing.EventStore;

public sealed class ConcorrenciaException(Guid aggregateId, long versaoEsperada, long versaoAtual)
    : Exception($"Conflito de concorrencia no aggregate {aggregateId}: esperava versao {versaoEsperada}, mas estava em {versaoAtual}.")
{
    public Guid AggregateId { get; } = aggregateId;
    public long VersaoEsperada { get; } = versaoEsperada;
    public long VersaoAtual { get; } = versaoAtual;
}
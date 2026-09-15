using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Events;

public sealed record CobrancaCriadaEvento(Guid CobrancaId, string CpfDevedor, decimal Valor) : Evento
{
    public override string NomeEvento => nameof(CobrancaCriadaEvento);
}

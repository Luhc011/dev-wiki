using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Events;

public sealed record DebitoRealizadoEvento(Guid CobrancaId, decimal Valor) : Evento
{
    public override string NomeEvento => nameof(DebitoRealizadoEvento);
}

using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Events;

public sealed record DebitoCanceladoEvento(Guid CobrancaId, decimal ValorDevolvido) : Evento
{
    public override string NomeEvento => nameof(DebitoCanceladoEvento);
}

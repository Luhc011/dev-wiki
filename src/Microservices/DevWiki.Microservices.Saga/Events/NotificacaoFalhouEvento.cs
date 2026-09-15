using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Events;

public sealed record NotificacaoFalhouEvento(Guid CobrancaId, string Motivo) : Evento
{
    public override string NomeEvento => nameof(NotificacaoFalhouEvento);
}
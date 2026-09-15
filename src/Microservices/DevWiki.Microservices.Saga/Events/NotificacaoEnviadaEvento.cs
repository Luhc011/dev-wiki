using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Events;

public sealed record NotificacaoEnviadaEvento(Guid CobrancaId, string Destinatario) : Evento
{
    public override string NomeEvento => nameof(NotificacaoEnviadaEvento);
}

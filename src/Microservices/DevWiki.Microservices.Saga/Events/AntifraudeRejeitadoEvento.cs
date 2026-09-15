using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Events;

public sealed record AntifraudeRejeitadoEvento(Guid CobrancaId, string Motivo) : Evento
{
    public override string NomeEvento => nameof(AntifraudeRejeitadoEvento);
}

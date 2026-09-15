using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Events;

public sealed record AntifraudeAprovadoEvento(Guid CobrancaId, string CpfDevedor) : Evento
{
    public override string NomeEvento => nameof(AntifraudeAprovadoEvento);
}

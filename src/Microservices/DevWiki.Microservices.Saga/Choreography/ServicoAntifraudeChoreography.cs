using DevWiki.Microservices.Saga.Events;
using DevWiki.Microservices.Saga.Interfaces;

namespace DevWiki.Microservices.Saga.Choreography;

public sealed class ServicoAntifraudeChoreography(IEventBus eventBus, IServicoAntifraude antifraude)
{
    public async Task ProcessarAsync(CobrancaCriadaEvento evento, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evento, nameof(evento));

        var resultado = await antifraude.AnalisarAsync(evento.CobrancaId, evento.CpfDevedor, evento.Valor, ct);

        if (!resultado.Sucesso)
        {
            await eventBus.PublicarAsync(new AntifraudeRejeitadoEvento(evento.CobrancaId, resultado.MensagemErro!), ct);
            return;
        }

        await eventBus.PublicarAsync(new AntifraudeAprovadoEvento(evento.CobrancaId, evento.CpfDevedor), ct);
    }
}
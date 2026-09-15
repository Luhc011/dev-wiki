using DevWiki.Microservices.Saga.Events;
using DevWiki.Microservices.Saga.Interfaces;

namespace DevWiki.Microservices.Saga.Choreography;

public sealed class ServicoDebitoChoreography(IEventBus eventBus, IServicoDebito debito)
{
    public async Task ProcessarAsync(AntifraudeAprovadoEvento evento, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evento, nameof(evento));

        var resultado = await debito.DebitarAsync(evento.CobrancaId, valor: 0m, ct);

        if (!resultado.Sucesso) return;

        await eventBus.PublicarAsync(new DebitoRealizadoEvento(evento.CobrancaId, Valor: 0m), ct);
    }

    public async Task CompensarAsync(NotificacaoFalhouEvento evento, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evento, nameof(evento));

        await debito.CancelarDebitoAsync(evento.CobrancaId, ct);

        await eventBus.PublicarAsync(new DebitoCanceladoEvento(evento.CobrancaId, ValorDevolvido: 0m), ct);
    }
}

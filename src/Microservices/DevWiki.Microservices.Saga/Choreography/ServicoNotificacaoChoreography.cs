using DevWiki.Microservices.Saga.Events;
using DevWiki.Microservices.Saga.Interfaces;

namespace DevWiki.Microservices.Saga.Choreography;

public sealed class ServicoNotificacaoChoreography(IEventBus eventBus, IServicoNotificacao notificacao)
{
    public async Task ProcessarAsync(DebitoRealizadoEvento evento, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evento, nameof(evento));

        var destinatario = $"cliente-{evento.CobrancaId:N}";
        var mensagem = $"Cobranca {evento.CobrancaId} processada com sucesso.";

        var resultado = await notificacao.NotificarAsync(evento.CobrancaId, destinatario, mensagem, ct);

        if (!resultado.Sucesso)
        {
            await eventBus.PublicarAsync(new NotificacaoFalhouEvento(evento.CobrancaId, resultado.MensagemErro!), ct);
            return;
        }

        await eventBus.PublicarAsync(new NotificacaoEnviadaEvento(evento.CobrancaId, destinatario), ct);
    }
}
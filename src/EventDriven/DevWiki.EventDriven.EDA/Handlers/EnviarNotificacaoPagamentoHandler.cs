using DevWiki.EventDriven.EDA.Events;
using DevWiki.EventDriven.EDA.Interfaces;

namespace DevWiki.EventDriven.EDA.Handlers;

public sealed class EnviarNotificacaoPagamentoHandler : IEventHandler<PagamentoProcessadoEvento>
{
    public int TotalNotificacoes { get; private set; }

    public Task HandleAsync(PagamentoProcessadoEvento evento, CancellationToken ct = default)
    {
        TotalNotificacoes++;
        return Task.CompletedTask;
    }
}
using DevWiki.EventDriven.EDA.Events;
using DevWiki.EventDriven.EDA.Interfaces;

namespace DevWiki.EventDriven.EDA.Handlers;

public sealed class AtualizarExtratoPagamentoHandler : IEventHandler<PagamentoProcessadoEvento>
{
    public int TotalAtualizacoes { get; private set; }

    public Task HandleAsync(PagamentoProcessadoEvento evento, CancellationToken ct = default)
    {
        TotalAtualizacoes++;
        return Task.CompletedTask;
    }
}

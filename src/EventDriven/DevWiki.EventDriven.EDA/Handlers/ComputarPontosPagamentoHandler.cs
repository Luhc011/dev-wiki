using DevWiki.EventDriven.EDA.Events;
using DevWiki.EventDriven.EDA.Interfaces;

namespace DevWiki.EventDriven.EDA.Handlers;

public sealed class ComputarPontosPagamentoHandler : IEventHandler<PagamentoProcessadoEvento>
{
    public int TotalPontosComputados { get; private set; }

    public Task HandleAsync(PagamentoProcessadoEvento evento, CancellationToken ct = default)
    {
        TotalPontosComputados++;
        return Task.CompletedTask;
    }
}

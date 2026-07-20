using DevWiki.EventDriven.EDA.Interfaces;
using DevWiki.EventDriven.Shared.Events;
using Microsoft.Extensions.DependencyInjection;

namespace DevWiki.EventDriven.EDA.EventBus;

public sealed class EventBusMemoria(IServiceProvider serviceProvider) : IEventBus
{
    public async Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default) where TEvento : DomainEvent
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvento>>();
        var tarefas = handlers.Select(h => h.HandleAsync(evento, ct));
        await Task.WhenAll(tarefas);
    }
}
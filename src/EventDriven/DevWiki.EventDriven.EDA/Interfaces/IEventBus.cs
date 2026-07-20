using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EDA.Interfaces;

public interface IEventBus
{
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default)
        where TEvento : DomainEvent;
}
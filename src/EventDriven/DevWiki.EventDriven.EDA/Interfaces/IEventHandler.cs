using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EDA.Interfaces;

public interface IEventHandler<TEvento> where TEvento : DomainEvent
{
    Task HandleAsync(TEvento evento, CancellationToken ct = default);
}

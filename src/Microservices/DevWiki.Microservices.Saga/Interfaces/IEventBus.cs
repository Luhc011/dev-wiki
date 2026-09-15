using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Interfaces;

public interface IEventBus
{
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default) where TEvento : Evento;

    Task<TEvento?> ReceberAsync<TEvento>(CancellationToken ct = default) where TEvento : Evento;
}
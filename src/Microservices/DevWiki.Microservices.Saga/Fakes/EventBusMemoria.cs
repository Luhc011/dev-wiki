using DevWiki.Microservices.Saga.Interfaces;
using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Fakes;

public sealed class EventBusMemoria : IEventBus
{
    private readonly Dictionary<string, Queue<Evento>> _filas = [];

    public int CapacidadeMaximaFila
    {
        get;
        init => field = value is > 0 and <= 100_000
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Capacidade deve ser entre 1 e 100.000.");
    } = 10_000;

    public int TotalPublicados => _filas.Values.Sum(q => q.Count);

    public Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default) where TEvento : Evento
    {
        ArgumentNullException.ThrowIfNull(evento, nameof(evento));
        var tipo = typeof(TEvento).Name;
        if (!_filas.TryGetValue(tipo, out var fila))
        {
            fila = new Queue<Evento>();
            _filas[tipo] = fila;
        }
        fila.Enqueue(evento);
        return Task.CompletedTask;
    }

    public Task<TEvento?> ReceberAsync<TEvento>(CancellationToken ct = default) where TEvento : Evento
    {
        var tipo = typeof(TEvento).Name;
        if (_filas.TryGetValue(tipo, out var fila) && fila.TryDequeue(out var evento))
            return Task.FromResult(evento as TEvento);
        return Task.FromResult<TEvento?>(null);
    }

    public int ContarPendentes<TEvento>() where TEvento : Evento
    {
        var tipo = typeof(TEvento).Name;
        return _filas.TryGetValue(tipo, out var fila) ? fila.Count : 0;
    }
}
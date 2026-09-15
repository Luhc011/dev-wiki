using DevWiki.Microservices.Outbox.Interfaces;
using Microsoft.Extensions.Hosting;

namespace DevWiki.Microservices.Outbox.Workers;

public sealed class OutboxWorker(IOutboxRepositorio repositorio, IPublicadorMensagens publicador) : BackgroundService
{
    public TimeSpan Intervalo
    {
        get;
        init => field = value > TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Intervalo deve ser positivo.");
    } = TimeSpan.FromSeconds(5);

    public async Task ProcessarUmaRodadaAsync(CancellationToken ct = default)
    {
        var pendentes = await repositorio.BuscarPendentesAsync(limite: 50, ct);

        foreach (var evento in pendentes)
        {
            var publicado = await publicador.PublicarAsync(evento, ct);

            if (publicado)
                await repositorio.MarcarComoProcessadoAsync(evento.Id, ct);
            else
                await repositorio.IncrementarTentativaAsync(evento.Id, ct);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessarUmaRodadaAsync(stoppingToken);
            await Task.Delay(Intervalo, stoppingToken);
        }
    }
}
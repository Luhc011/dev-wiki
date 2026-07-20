using DevWiki.EventDriven.EDA.Events;
using DevWiki.EventDriven.EDA.Interfaces;

namespace DevWiki.EventDriven.EDA.Handlers;

public sealed class AlertarFraudeHandler : IEventHandler<FraudeDetectadaEvento>
{
    public int TotalAlertas { get; private set; }
    public Task HandleAsync(FraudeDetectadaEvento evento, CancellationToken ct = default)
    {
        TotalAlertas++;
        return Task.CompletedTask;
    }
}
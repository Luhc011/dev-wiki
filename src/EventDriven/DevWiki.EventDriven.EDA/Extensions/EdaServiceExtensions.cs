using DevWiki.EventDriven.EDA.EventBus;
using DevWiki.EventDriven.EDA.Events;
using DevWiki.EventDriven.EDA.Handlers;
using DevWiki.EventDriven.EDA.Interfaces;
using DevWiki.EventDriven.EDA.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevWiki.EventDriven.EDA.Extensions;

public static class EdaServiceExtensions
{
    public static void AddEdaServices(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, EventBusMemoria>();
        services.AddSingleton<ServicoProcessamentoPagamento>();

        services.AddSingleton<IEventHandler<PagamentoProcessadoEvento>, EnviarNotificacaoPagamentoHandler>();
        services.AddSingleton<IEventHandler<PagamentoProcessadoEvento>, AtualizarExtratoPagamentoHandler>();
        services.AddSingleton<IEventHandler<PagamentoProcessadoEvento>, ComputarPontosPagamentoHandler>();
        services.AddSingleton<IEventHandler<FraudeDetectadaEvento>, AlertarFraudeHandler>();
    }
}
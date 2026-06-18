using DevWiki.Solid.Dip.Correto.Implementacoes;
using DevWiki.Solid.Dip.Correto.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DevWiki.Solid.Dip.Correto;

public class ConfiguracaoDi
{
    public static IServiceProvider CriarContainer()
    {
        var services = new ServiceCollection();

        services.AddScoped<IAnalisadorAntifraude, AnalisadorAntifraudeSerpro>();
        services.AddScoped<IGatewayPagamento>(_ => new GatewayPixBacen { PrefixoTransacao = "PIX-BACEN" });
        services.AddScoped<IRepositorioPagamento, RepositorioPagamentoMemoria>();
        services.AddScoped<INotificadorPagamento, NotificadorConsole>();

        services.AddScoped<ServicoAutorizacaoPagamento>();

        return services.BuildServiceProvider();
    }
}

using DevWiki.CleanArch.Domain.Interfaces;
using DevWiki.CleanArch.Infrasctructure.Notifications;
using DevWiki.CleanArch.Infrasctructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DevWiki.CleanArch.Infrasctructure.Extensions;

public static class InfraServiceExtensions
{
    public static IServiceCollection AddInfraServices(this IServiceCollection services)
    {
        services.AddSingleton<IRepositorioCobranca>(_ => new RepositorioCobrancaMemoria { CapacidadeMax = 50_000 });

        services.AddScoped<INotificadorCobranca, NotificadorConsole>();
        return services;
    }
}

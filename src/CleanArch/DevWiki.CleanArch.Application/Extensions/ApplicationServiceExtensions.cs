using DevWiki.CleanArch.Application.Abstractions;
using DevWiki.CleanArch.Application.UseCases.ConsultarCobranca;
using DevWiki.CleanArch.Application.UseCases.ProcessarCobranca;
using DevWiki.CleanArch.Application.UseCases.RegistrarPagamento;
using Microsoft.Extensions.DependencyInjection;

namespace DevWiki.CleanArch.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<ProcessarCobrancaCommand, ProcessarCobrancaResult>, ProcessarCobrancaHandler>();

        services.AddScoped<IQueryHandler<ConsultarCobrancaQuery, ConsultarCobrancaResult>, ConsultarCobrancaHandler>();

        services.AddScoped<ICommandHandler<RegistrarPagamentoCommand, RegistrarPagamentoResult>, RegistrarPagamentoHandler>();

        return services;
    }
}

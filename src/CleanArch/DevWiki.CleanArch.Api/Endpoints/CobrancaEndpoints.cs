using DevWiki.CleanArch.Api.Dtos;
using DevWiki.CleanArch.Application.Abstractions;
using DevWiki.CleanArch.Application.UseCases.ConsultarCobranca;
using DevWiki.CleanArch.Application.UseCases.ProcessarCobranca;
using DevWiki.CleanArch.Application.UseCases.RegistrarPagamento;

namespace DevWiki.CleanArch.Api.Endpoints;

public static class CobrancaEndpoints
{
    public static WebApplication MapCobrancaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cobrancas");

        group.MapPost("/", ProcessarCobranca).WithName("ProcessarCobranca");
        group.MapGet("/{id:guid}", ConsultarCobranca).WithName("ConsultarCobranca");
        group.MapPost("/{id:guid}/pagamentos", RegistrarPagamento).WithName("RegistrarPagamento");

        return app;
    }

    private static async Task<IResult> ProcessarCobranca(ProcessarCobrancaRequest request,
                                                         ICommandHandler<ProcessarCobrancaCommand, ProcessarCobrancaResult> handler,
                                                         CancellationToken ct)
    {
        var command = new ProcessarCobrancaCommand
        {
            CpfDevedor = request.CpfDevedor,
            ValorTotal = request.Valor,
            Descricao = request.Descricao,
            DataVencimento = request.DataVencimento
        };

        var result = await handler.HandleAsync(command, ct);

        return result.Sucesso
            ? Results.Created($"/cobrancas/{result.Valor!.CobrancaId.Valor}", result.Valor)
            : Results.UnprocessableEntity(new { erro = result.MensagemErro });
    }

    private static async Task<IResult> ConsultarCobranca(Guid id,
                                                         IQueryHandler<ConsultarCobrancaQuery, ConsultarCobrancaResult> handler,
                                                         CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ConsultarCobrancaQuery(id), ct);

        return result.Sucesso
            ? Results.Ok(CobrancaResponse.De(result.Valor!))
            : Results.NotFound(new { erro = result.MensagemErro });
    }

    private static async Task<IResult> RegistrarPagamento(Guid id,
                                                          RegistrarPagamentoRequest request,
                                                          ICommandHandler<RegistrarPagamentoCommand, RegistrarPagamentoResult> handler,
                                                          CancellationToken ct)
    {
        var command = new RegistrarPagamentoCommand
        {
            CobrancaId = id,
            ValorPago = request.Valor,
            DataHora = DateTimeOffset.UtcNow
        };

        var result = await handler.HandleAsync(command, ct);

        return result.Sucesso
            ? Results.Ok(result.Valor)
            : Results.UnprocessableEntity(new { erro = result.MensagemErro });
    }
}
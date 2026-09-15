using DevWiki.Microservices.CircuitBreaker.Interfaces;
using DevWiki.Microservices.CircuitBreaker.Models;
using Polly;

namespace DevWiki.Microservices.CircuitBreaker.Services;

public sealed class GatewayPagamentoResistente(IServicoBacen bacen, ResiliencePipeline<ResultadoPagamento> pipeline)
{
    public ValueTask<ResultadoPagamento> ProcessarAsync(Guid cobrancaId, decimal valor, CancellationToken ct = default) =>
        pipeline.ExecuteAsync(async token => await bacen.ProcessarAsync(cobrancaId, valor, token), ct);
}

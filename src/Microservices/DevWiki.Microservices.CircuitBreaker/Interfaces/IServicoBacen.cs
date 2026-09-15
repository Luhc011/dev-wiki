using DevWiki.Microservices.CircuitBreaker.Models;

namespace DevWiki.Microservices.CircuitBreaker.Interfaces;

public interface IServicoBacen
{
    Task<ResultadoPagamento> ProcessarAsync(Guid cobrancaId, decimal valor, CancellationToken ct = default);
}

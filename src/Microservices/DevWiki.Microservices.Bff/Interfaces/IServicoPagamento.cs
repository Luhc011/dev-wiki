using DevWiki.Microservices.Bff.Models;

namespace DevWiki.Microservices.Bff.Interfaces;

public interface IServicoPagamento
{
    Task<IReadOnlyList<PagamentoDetalhe>> BuscarPagamentosAsync(Guid cobrancaId, CancellationToken ct = default);
}
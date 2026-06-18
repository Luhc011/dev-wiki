using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Dip.Correto.Interfaces;

public interface IGatewayPagamento
{
    Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao);
}

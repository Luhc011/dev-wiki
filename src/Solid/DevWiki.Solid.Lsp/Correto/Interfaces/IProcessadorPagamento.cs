using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Correto.Interfaces;

public interface IProcessadorPagamento
{
    Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao);

    Task<StatusPagamento> ConsultarStatusAsync(string idTransacao);
}
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Violacao.Interfaces;

public interface IProcessadorPagamento
{
    Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao);
    Task<Comprovante> GerarComprovanteAsync(string idTransacao);
    Task CancelarAsync(string idTransacao);
    Task<StatusPagamento> ConsultarStatusAsync(string idTransacao);
}

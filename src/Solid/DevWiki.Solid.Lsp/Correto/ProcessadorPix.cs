using DevWiki.Solid.Lsp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Correto;

public class ProcessadorPix : IProcessadorCancelavel, IProcessadorComComprovante
{
    public Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
        => Task.FromResult(new ResultadoPagamento(Guid.NewGuid().ToString("N"), StatusPagamento.Processado, null));

    public Task<Comprovante> GerarComprovanteAsync(string idTransacao)
        => Task.FromResult(new Comprovante(idTransacao, DateTimeOffset.Now, 0m, "pix processado com ssucesso"));

    public Task<StatusPagamento> ConsultarStatusAsync(string idTransacao)
        => Task.FromResult(StatusPagamento.Processado);

    public async Task CancelarAsync(string idTransacao)
        => await Task.Delay(15);
}

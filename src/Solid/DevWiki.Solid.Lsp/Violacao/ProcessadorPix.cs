using DevWiki.Solid.Lsp.Violacao.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Violacao;

public class ProcessadorPix : IProcessadorPagamento
{
    public Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
    {
        var resultado = new ResultadoPagamento(Guid.NewGuid().ToString("N"), StatusPagamento.Processado, null);
        return Task.FromResult(resultado);
    }

    public Task<Comprovante> GerarComprovanteAsync(string idTransacao)
    {
        var comprovante = new Comprovante(idTransacao, DateTimeOffset.Now, 0, "pix processado");

        return Task.FromResult(comprovante);
    }

    public async Task CancelarAsync(string idTransacao)
    {
        await Task.Delay(10);
    }

    public Task<StatusPagamento> ConsultarStatusAsync(string idTransacao)
        => Task.FromResult(StatusPagamento.Processado);
}

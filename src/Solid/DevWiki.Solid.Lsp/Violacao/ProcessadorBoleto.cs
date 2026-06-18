using DevWiki.Solid.Lsp.Violacao.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Violacao;

public class ProcessadorBoleto : IProcessadorPagamento
{
    public Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
        => Task.FromResult(new ResultadoPagamento(null, StatusPagamento.AguardandoCompensacao, "aguardando pagamento"));

    public Task<Comprovante> GerarComprovanteAsync(string idTransacao)
        => Task.FromResult<Comprovante>(null!);

    public Task<StatusPagamento> ConsultarStatusAsync(string idTransacao)
        => throw new NotImplementedException("boleto n pode ser cancelado");

    public Task CancelarAsync(string idTransacao)
        => Task.FromResult(StatusPagamento.Cancelado);
}

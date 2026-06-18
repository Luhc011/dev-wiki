using DevWiki.Solid.Lsp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Correto;

public class ProcessadorBoleto : IProcessadorPagamento
{
    public Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
    {
        var numeroBoleto = $"BOL-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";

        return Task.FromResult(new ResultadoPagamento(numeroBoleto,
                                                      StatusPagamento.AguardandoCompensacao,
                                                      "boleto registrado. aguardando pag pelo cliente"));
    }

    public Task<StatusPagamento> ConsultarStatusAsync(string idTransacao)
        => Task.FromResult(StatusPagamento.AguardandoCompensacao);

    public async Task InutilizarAsync(string numeroBoleto)
    {
        await Task.Delay(15);
        Console.WriteLine($"[boleto] boleto num {numeroBoleto} inutilizado");
    }
}
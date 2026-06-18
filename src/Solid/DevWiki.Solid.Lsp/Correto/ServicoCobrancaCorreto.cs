using DevWiki.Solid.Lsp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Correto;

public class ServicoCobrancaCorreto(IProcessadorPagamento processador)
{
    public async Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
    {
        if (processador is IProcessadorComJanela janela && !janela.EstaDisponivel(DateTimeOffset.Now))
            throw new InvalidOperationException($"processamento indisponivel. janela de processamento: {janela.DescricaoJanela}");

        var resultado = await processador.ProcessarAsync(solicitacao);

        Console.WriteLine($"[log] transacao: {resultado.IdTransacao} | status: {resultado.Status}");
        return resultado;
    }

    public async Task EnviarComprovanteAsync(IProcessadorComComprovante processadorComComprovante, string idTransacao)
    {
        var comprovante = await processadorComComprovante.GerarComprovanteAsync(idTransacao);
        Console.WriteLine($"[comprovante] transacao: {comprovante.IdTransacao} em {comprovante.DataHora}");
    }

    public async Task CancelarAsync(IProcessadorCancelavel processadorCancelavel, string idTransacao)
    {
        await processadorCancelavel.CancelarAsync(idTransacao);
    }
}
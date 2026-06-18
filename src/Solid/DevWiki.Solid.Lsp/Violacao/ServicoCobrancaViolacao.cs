using DevWiki.Solid.Lsp.Violacao.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Violacao;

public class ServicoCobrancaViolacao
{
    public async Task<ResultadoPagamento> ProcessarCobrancaAsync(IProcessadorPagamento processador, SolicitacaoPagamento solicitacao)
    {
        if (processador is ProcessadorTed && !EhHorarioBancario())
            throw new InvalidOperationException("ted n esta disponivel neste horario");

        if (processador is ProcessadorBoleto)
            Console.WriteLine("info boleto: result sera AguardandoCompensacao");

        var resultado = await processador.ProcessarAsync(solicitacao);

        if (resultado.IdTransacao is not null)
            Console.WriteLine($"[log] transacao gerada: {resultado.IdTransacao}");
        else
            Console.WriteLine($"[log] nenhuma transacao gerada");

        return resultado;
    }

    public async Task CancelarAsync(IProcessadorPagamento processador, string idTransacao)
    {
        try
        {
            await processador.CancelarAsync(idTransacao);
        }
        catch (NotSupportedException ex)
        {
            Console.WriteLine($"[aviso] cancelamento n suportado p {processador.GetType().Name}: {ex.Message}");
        }
    }

    private static bool EhHorarioBancario()
    {
        var hora = TimeOnly.FromDateTime(DateTime.Now);

        return hora >= new TimeOnly(6, 0)
            && hora < new TimeOnly(17, 0)
            && DateTime.Now.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
    }
}
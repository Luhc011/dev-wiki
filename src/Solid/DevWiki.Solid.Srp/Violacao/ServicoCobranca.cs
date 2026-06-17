using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Srp.Violacao;

public class ServicoCobranca
{
    public async Task<ResultadoPagamento> ProcessarCobrancaAsync(SolicitacaoPagamento solicitacao)
    {
        if (!ValidarSolicitacao(solicitacao))
            return new ResultadoPagamento(null, StatusPagamento.Cancelado, "solicitacao invalida");


        await Task.Delay(5);

        var resultado = new ResultadoPagamento(Guid.NewGuid().ToString("N"), StatusPagamento.Processado, "pagamento aprovado");

        await SalvarNoBancoAsync(resultado);
        await EnviarNotificacaoAsync("luk.com.br", resultado);

        return resultado;
    }

    public string GerarRelatorioCobranca(IEnumerable<ResultadoPagamento> resultados)
    {
        var processados = resultados.Where(r => r.Status == StatusPagamento.Processado);
        var total = processados.Count();

        var linhas = resultados.Select(r => $"transacao: {r.IdTransacao} - status: {r.Status}");

        return $"total de cobrancas processadas: {total}\n" + string.Join("\n", linhas);
    }

    private bool ValidarSolicitacao(SolicitacaoPagamento solicitacao) =>
        solicitacao.Valor > 0
        && !string.IsNullOrEmpty(solicitacao.BancoOrigem)
        && !string.IsNullOrEmpty(solicitacao.Descricao);

    private async Task SalvarNoBancoAsync(ResultadoPagamento resultado)
    {
        await Task.Delay(5);
        Console.WriteLine("gravando transacao {0} - status: {1}", resultado.IdTransacao, resultado.Status);
    }

    private async Task EnviarNotificacaoAsync(string email, ResultadoPagamento resultado)
    {
        var msg = $"""
            cobranca processada
            transacao: {resultado.IdTransacao}
            statts: {resultado.Status}
            banco holkannda hehe
            """;

        await Task.Delay(5);

        Console.WriteLine("enviando p {0}:\n{1}", email, msg);
    }
}

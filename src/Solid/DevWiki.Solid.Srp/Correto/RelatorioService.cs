using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Srp.Correto;

public class RelatorioService : IRelatorioService
{
    public string Gerar(IEnumerable<ResultadoPagamento> resultados)
    {
        var processados = resultados.Where(r => r.Status == StatusPagamento.Processado);
        var linhas = resultados.Select(r => $"transacao: {r.IdTransacao} - status: {r.Status}");

        return $"total de cobrancas processadas: {processados.Count()}\n" + string.Join("\n", linhas);
    }
}

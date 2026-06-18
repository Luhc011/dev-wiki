using DevWiki.Solid.Isp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Isp.Correto;

public class RelatorioCobrancaService(IRepositorioCobrancaLeitura repositorio) : IRelatorioCobrancaService
{
    public async Task<RelatorioCobranca> GerarAsync(DateOnly periodo)
    {
        var cobrancas = await repositorio.ListarPorStatusAsync(StatusPagamento.Processado);
        var total = cobrancas.Sum(c => c.Valor);
        return new RelatorioCobranca(periodo, [.. cobrancas], total);
    }
}

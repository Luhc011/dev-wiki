using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Isp.Correto.Interfaces;

public interface IRelatorioCobrancaService
{
    Task<RelatorioCobranca> GerarAsync(DateOnly periodo);
}

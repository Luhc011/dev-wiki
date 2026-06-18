using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Isp.Violacao.Interfaces;

public interface IRepositorioCobranca
{
    Task<Cobranca?> BuscarPorIdAsync(string id);
    Task<IReadOnlyList<Cobranca>> ListarPorStatusAsync(StatusPagamento status);
    Task SalvarAsync(Cobranca cobranca);
    Task<RelatorioCobranca> GerarRelatorioAsync(DateOnly periodo);
    Task<string> ExportarCsvAsync();
    Task EnviarNotificacoesPendentesAsync();
}
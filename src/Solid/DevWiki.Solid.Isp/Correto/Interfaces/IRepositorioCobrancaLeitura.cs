using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Isp.Correto.Interfaces;

public interface IRepositorioCobrancaLeitura
{
    Task<Cobranca?> BuscarPorIdAsync(string id);

    Task<IReadOnlyList<Cobranca>> ListarPorStatusAsync(StatusPagamento status);
}
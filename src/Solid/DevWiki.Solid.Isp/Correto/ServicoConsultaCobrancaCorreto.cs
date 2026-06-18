using DevWiki.Solid.Isp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Isp.Correto;

public class ServicoConsultaCobrancaCorreto(IRepositorioCobrancaLeitura repositorio)
{
    public Task<Cobranca?> BuscarAsync(string id)
        => repositorio.BuscarPorIdAsync(id);

    public Task<IReadOnlyList<Cobranca>> ListarPendentesAsync()
        => repositorio.ListarPorStatusAsync(StatusPagamento.Pendente);
}
using DevWiki.Solid.Isp.Violacao.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Isp.Violacao;

public sealed class ServicoConsultaCobranca(IRepositorioCobranca repositorio)
{
    public Task<Cobranca?> BuscarAsync(string id)
        => repositorio.BuscarPorIdAsync(id);

    public Task<IReadOnlyList<Cobranca>> ListarPendentesAsync()
        => repositorio.ListarPorStatusAsync(StatusPagamento.Pendente);
}
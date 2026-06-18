using DevWiki.Solid.Isp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Isp.Correto;

public class RepositorioCobrancaEscrita : IRepositorioCobrancaEscrita
{
    private readonly Dictionary<string, (Cobranca Cobranca, StatusPagamento Status)> _dados = [];
    public int Count => _dados.Count;

    public Task SalvarAsync(Cobranca cobranca, StatusPagamento status)
    {
        _dados[cobranca.Id] = (cobranca, status);
        return Task.CompletedTask;
    }
}

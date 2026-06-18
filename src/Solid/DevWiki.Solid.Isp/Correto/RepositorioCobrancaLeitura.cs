using DevWiki.Solid.Isp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Isp.Correto;

internal record RegistroCobranca(Cobranca Cobranca, StatusPagamento Status);
public class RepositorioCobrancaLeitura : IRepositorioCobrancaLeitura
{
    private readonly List<RegistroCobranca> _registros = [];

    public int PaginaTamanho
    {
        get;
        init => field = value is > 0 and <= 1000
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "tamanho de pag deve estar entre 1 e 1000");
    } = 50;

    public void Adicionar(Cobranca cobranca, StatusPagamento status)
        => _registros.Add(new RegistroCobranca(cobranca, status));

    public Task<Cobranca?> BuscarPorIdAsync(string id)
    {
        var cobranca = _registros.FirstOrDefault(r => r.Cobranca.Id == id)?.Cobranca;
        return Task.FromResult(cobranca);
    }

    public Task<IReadOnlyList<Cobranca>> ListarPorStatusAsync(StatusPagamento status)
    {
        IReadOnlyList<Cobranca> resultado = [.. _registros
            .Where(r => r.Status == status)
            .Select(r => r.Cobranca)];

        return Task.FromResult(resultado);
    }
}

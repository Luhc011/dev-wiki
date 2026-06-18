using DevWiki.Solid.Dip.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Dip.Correto.Implementacoes;

public sealed class RepositorioPagamentoMemoria : IRepositorioPagamento
{
    private readonly Dictionary<string, ResultadoPagamento> _dados = [];
    public int Count => _dados.Count;
    public int CapacidadeMaxima
    {
        get;
        init => field = value is > 0 and <= 100_000
            ? value
            : throw new ArgumentOutOfRangeException(
                nameof(value), "capacidade deve estar entre 1 e 100.000 registros");
    } = 10_000;

    public Task SalvarAsync(ResultadoPagamento resultado)
    {
        if (resultado.IdTransacao is not null)
            _dados[resultado.IdTransacao] = resultado;

        return Task.CompletedTask;
    }

    public Task<ResultadoPagamento?> BuscarAsync(string idTransacao)
    {
        _dados.TryGetValue(idTransacao, out var resultado);
        return Task.FromResult(resultado);
    }
}
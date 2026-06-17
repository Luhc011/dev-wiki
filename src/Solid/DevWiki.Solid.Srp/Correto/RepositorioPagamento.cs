using DevWiki.Solid.Shared.Domain;
using System.Collections.Concurrent;

namespace DevWiki.Solid.Srp.Correto;

public class RepositorioPagamento : IRepositorioPagamento
{
    private readonly ConcurrentDictionary<string, ResultadoPagamento> _inMemoryStore = new();
    public Task<ResultadoPagamento?> BuscarAsync(string idTransacao)
    {
        _inMemoryStore.TryGetValue(idTransacao, out var result);

        return Task.FromResult(result);
    }

    public Task SalvarAsync(ResultadoPagamento resultado)
    {
        if (resultado.IdTransacao is not null)
            _inMemoryStore[resultado.IdTransacao] = resultado;

        return Task.FromResult(resultado);
    }
}

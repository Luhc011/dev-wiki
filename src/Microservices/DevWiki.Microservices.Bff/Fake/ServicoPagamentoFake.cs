using DevWiki.Microservices.Bff.Interfaces;
using DevWiki.Microservices.Bff.Models;

namespace DevWiki.Microservices.Bff.Fake;

public sealed class ServicoPagamentoFake : IServicoPagamento
{
    private readonly Dictionary<Guid, List<PagamentoDetalhe>> _pagamentos = [];

    public void AdicionarPagamento(PagamentoDetalhe pagamento)
    {
        if (!_pagamentos.TryGetValue(pagamento.CobrancaId, out var lista))
        {
            lista = [];
            _pagamentos[pagamento.CobrancaId] = lista;
        }
        lista.Add(pagamento);
    }

    public Task<IReadOnlyList<PagamentoDetalhe>> BuscarPagamentosAsync(Guid cobrancaId, CancellationToken ct = default)
    {
        IReadOnlyList<PagamentoDetalhe> resultado = _pagamentos.TryGetValue(cobrancaId, out var lista)
            ? [.. lista]
            : [];

        return Task.FromResult(resultado);
    }
}
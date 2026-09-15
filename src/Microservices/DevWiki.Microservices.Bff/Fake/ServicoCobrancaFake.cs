using DevWiki.Microservices.Bff.Interfaces;
using DevWiki.Microservices.Bff.Models;

namespace DevWiki.Microservices.Bff.Fake;

public sealed class ServicoCobrancaFake : IServicoCobranca
{
    private readonly Dictionary<Guid, CobrancaDetalhe> _cobrancas = [];
    public void AdicionarCobranca(CobrancaDetalhe cobranca) => _cobrancas[cobranca.Id] = cobranca;

    public Task<CobrancaDetalhe?> BuscarCobrancaAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_cobrancas.GetValueOrDefault(id));
}
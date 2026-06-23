using DevWiki.CleanArch.Domain.Entities;
using DevWiki.CleanArch.Domain.Enums;
using DevWiki.CleanArch.Domain.Interfaces;
using DevWiki.CleanArch.Domain.SharedKernel;

namespace DevWiki.CleanArch.Infrasctructure.Repositories;

internal sealed class RepositorioCobrancaMemoria : IRepositorioCobranca
{
    private readonly Dictionary<CobrancaId, Cobranca> _dados = [];
    public int CapacidadeMax
    {
        get;
        init => field = value is > 0 and <= 100_00
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "capacidade deve estar entre 1 e 100.000 registros");
    } = 10_000;

    public Task<Cobranca?> BuscarPorIdAsync(CobrancaId id, CancellationToken ct = default)
    {
        _dados.TryGetValue(id, out var cobranca);
        return Task.FromResult(cobranca);
    }

    public Task<IReadOnlyList<Cobranca>> ListarPorStatusAsync(StatusCobranca status, CancellationToken ct = default)
    {
        IReadOnlyList<Cobranca> resultado = [.. _dados.Values.Where(x => x.Status == status)];
        return Task.FromResult(resultado);
    }

    public Task SalvarAsync(Cobranca cobranca, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cobranca, nameof(cobranca));
        _dados[cobranca.CobrancaId] = cobranca;
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(Cobranca cobranca, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cobranca, nameof(cobranca));
        if (!_dados.ContainsKey(cobranca.CobrancaId))
            throw new KeyNotFoundException($"cobranca com id {cobranca.CobrancaId} n encontrada");

        _dados[cobranca.CobrancaId] = cobranca;
        return Task.CompletedTask;
    }
}
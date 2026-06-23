using DevWiki.CleanArch.Application.Abstractions;
using DevWiki.CleanArch.Domain.Interfaces;
using DevWiki.CleanArch.Domain.SharedKernel;
using DevWiki.CleanArch.Domain.ValueObjects;

namespace DevWiki.CleanArch.Application.UseCases.ConsultarCobranca;

public sealed class ConsultarCobrancaHandler(IRepositorioCobranca repositorio)
    : IQueryHandler<ConsultarCobrancaQuery, ConsultarCobrancaResult>
{
    public async Task<Result<ConsultarCobrancaResult>> HandleAsync(ConsultarCobrancaQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var cobrancaId = CobrancaId.De(query.CobrancaId);
        var cobranca = await repositorio.BuscarPorIdAsync(cobrancaId, ct);

        if (cobranca is null)
            return Result<ConsultarCobrancaResult>.Falha($"cobrança {query.CobrancaId} n encontrada.");

        var result = new ConsultarCobrancaResult(
            cobranca.CobrancaId,
            cobranca.CpfDevedor.Formatado,
            cobranca.ValorTotal.Quantia,
            cobranca.Descricao,
            cobranca.DataVencimento,
            cobranca.Status,
            [.. cobranca.Pagamentos.Select(p =>
                new ConsultarCobrancaResult.PagamentoResult(p.Id, p.Valor.Quantia, p.DataHora))]);

        return Result<ConsultarCobrancaResult>.Ok(result);
    }
}
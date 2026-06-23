using DevWiki.CleanArch.Domain.Enums;
using DevWiki.CleanArch.Domain.SharedKernel;

namespace DevWiki.CleanArch.Application.UseCases.ConsultarCobranca;

public sealed record ConsultarCobrancaResult(
    CobrancaId CobrancaId,
    string CpfDevedor,
    decimal ValorTotal,
    string Descricao,
    DateOnly DataVencimento,
    StatusCobranca Status,
    IReadOnlyList<ConsultarCobrancaResult.PagamentoResult> Pagamentos)
{
    public sealed record PagamentoResult(Guid PagamentoId, decimal Valor, DateTimeOffset DataHora);
}
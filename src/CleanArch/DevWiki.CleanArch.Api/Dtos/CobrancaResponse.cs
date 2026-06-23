using DevWiki.CleanArch.Application.UseCases.ConsultarCobranca;

namespace DevWiki.CleanArch.Api.Dtos;

public sealed record CobrancaResponse(
    Guid CobrancaId,
    string CpfDevedor,
    decimal ValorTotal,
    string Descricao,
    DateOnly DataVencimento,
    string Status,
    IReadOnlyList<CobrancaResponse.PagamentoResponse> Pagamentos)
{
    public sealed record PagamentoResponse(Guid PagamentoId, decimal Valor, DateTimeOffset DataHora);

    public static CobrancaResponse De(ConsultarCobrancaResult result) =>
        new(result.CobrancaId.Valor,
            result.CpfDevedor,
            result.ValorTotal,
            result.Descricao,
            result.DataVencimento,
            result.Status.ToString(),
            [.. result.Pagamentos.Select(p => new PagamentoResponse(p.PagamentoId, p.Valor, p.DataHora))]);
}
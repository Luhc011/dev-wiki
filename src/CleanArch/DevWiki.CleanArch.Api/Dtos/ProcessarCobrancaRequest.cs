namespace DevWiki.CleanArch.Api.Dtos;

public sealed record ProcessarCobrancaRequest
{
    public required string CpfDevedor { get; init; }
    public required decimal Valor { get; init; }
    public required string Descricao { get; init; }
    public required DateOnly DataVencimento { get; init; }
}

public sealed record RegistrarPagamentoRequest
{
    public required decimal Valor { get; init; }
}
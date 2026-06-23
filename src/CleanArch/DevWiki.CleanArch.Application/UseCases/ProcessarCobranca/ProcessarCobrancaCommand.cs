namespace DevWiki.CleanArch.Application.UseCases.ProcessarCobranca;

public sealed record ProcessarCobrancaCommand
{
    public required string CpfDevedor { get; init; }
    public required decimal ValorTotal { get; init; }
    public required string Descricao { get; init; }
    public required DateOnly DataVencimento { get; init; }
}
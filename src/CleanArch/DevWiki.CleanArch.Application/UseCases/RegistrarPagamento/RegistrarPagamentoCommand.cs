namespace DevWiki.CleanArch.Application.UseCases.RegistrarPagamento;

public sealed record RegistrarPagamentoCommand
{
    public required Guid CobrancaId { get; init; }
    public required decimal ValorPago { get; init; }
    public required DateTimeOffset DataHora { get; init; }
}
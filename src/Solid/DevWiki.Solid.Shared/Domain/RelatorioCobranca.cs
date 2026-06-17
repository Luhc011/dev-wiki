namespace DevWiki.Solid.Shared.Domain;

public record RelatorioCobranca(DateOnly Periodo, IReadOnlyList<Cobranca> Cobrancas, decimal TotalProcessado);

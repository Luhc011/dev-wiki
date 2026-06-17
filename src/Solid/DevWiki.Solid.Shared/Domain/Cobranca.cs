namespace DevWiki.Solid.Shared.Domain;

public record Cobranca(string Id, decimal Valor, string Descricao, DateOnly DataVencimento);

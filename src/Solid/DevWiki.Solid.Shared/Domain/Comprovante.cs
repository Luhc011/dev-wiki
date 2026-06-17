namespace DevWiki.Solid.Shared.Domain;

public record Comprovante(string IdTransacao, DateTimeOffset DataHora, decimal Valor, string Descricao);

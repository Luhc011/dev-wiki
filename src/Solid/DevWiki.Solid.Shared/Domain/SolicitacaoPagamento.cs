namespace DevWiki.Solid.Shared.Domain;

public record SolicitacaoPagamento(string IdSolicitacao, decimal Valor, string Descricao, string BancoOrigem);

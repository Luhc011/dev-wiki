namespace DevWiki.Solid.Shared.Domain;

public record ResultadoPagamento(string? IdTransacao, StatusPagamento Status, object? Detalhes);

public enum StatusPagamento
{
    Indefinido,
    Pendente,
    Processado,
    AguardandoCompensacao,
    Compensado,
    Cancelado
}
namespace DevWiki.Microservices.CircuitBreaker.Models;

public sealed record ResultadoPagamento(Guid CobrancaId, string CodigoAutorizacao)
{
    public bool Aprovado => CodigoAutorizacao is not (null or "DEGRADADO" or "");
    public static ResultadoPagamento Degradado() => new(Guid.Empty, "DEGRADADO");
}

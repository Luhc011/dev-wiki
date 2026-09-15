namespace DevWiki.Microservices.Bff.Models;

public sealed record PagamentoDetalhe(Guid Id,
                                      Guid CobrancaId,
                                      decimal Valor,
                                      DateTimeOffset RealizadoEm,
                                      string CodigoAutorizacao);
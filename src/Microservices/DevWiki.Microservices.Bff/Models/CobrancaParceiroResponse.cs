namespace DevWiki.Microservices.Bff.Models;

public sealed record CobrancaParceiroResponse(string IdExterno,
                                              decimal Valor,
                                              string Situacao);

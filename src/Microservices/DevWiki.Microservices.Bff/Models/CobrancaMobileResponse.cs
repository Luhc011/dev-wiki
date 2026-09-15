namespace DevWiki.Microservices.Bff.Models;

public sealed record CobrancaMobileResponse(Guid Id,
                                            decimal Valor,
                                            string Status);

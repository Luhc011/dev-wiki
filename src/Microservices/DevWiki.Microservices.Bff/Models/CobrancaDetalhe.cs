namespace DevWiki.Microservices.Bff.Models;

public sealed record CobrancaDetalhe(Guid Id,
                                     string CpfDevedor,
                                     decimal Valor,
                                     string Moeda,
                                     string Status,
                                     DateTimeOffset CriadaEm);
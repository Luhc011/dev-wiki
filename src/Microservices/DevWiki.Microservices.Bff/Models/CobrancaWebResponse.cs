namespace DevWiki.Microservices.Bff.Models;

public sealed record CobrancaWebResponse(Guid Id,
                                         string CpfDevedor,
                                         decimal Valor,
                                         string Moeda,
                                         string Status,
                                         DateTimeOffset CriadaEm,
                                         IReadOnlyList<PagamentoDetalhe> Pagamentos);

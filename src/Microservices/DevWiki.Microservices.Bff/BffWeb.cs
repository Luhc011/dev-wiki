using DevWiki.Microservices.Bff.Interfaces;
using DevWiki.Microservices.Bff.Models;
using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Bff;

public sealed class BffWeb(IServicoCobranca servico, IServicoPagamento servicoPagamento)
{
    public async Task<Result<CobrancaWebResponse>> BuscarCobrancaAsync(Guid id, CancellationToken ct = default)
    {
        var cobranca = await servico.BuscarCobrancaAsync(id, ct);
        if (cobranca is null)
            return Result<CobrancaWebResponse>.Falha($"Cobranca {id} n encontrada.");

        var pagamentos = await servicoPagamento.BuscarPagamentosAsync(id, ct);

        return Result<CobrancaWebResponse>.Ok(new CobrancaWebResponse(cobranca.Id,
                                                                      cobranca.CpfDevedor,
                                                                      cobranca.Valor,
                                                                      cobranca.Moeda,
                                                                      cobranca.Status,
                                                                      cobranca.CriadaEm,
                                                                      pagamentos));
    }
}
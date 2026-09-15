using DevWiki.Microservices.Bff.Interfaces;
using DevWiki.Microservices.Bff.Models;
using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Bff;

public sealed class BffParceiro(IServicoCobranca servico)
{
    public async Task<Result<CobrancaParceiroResponse>> BuscarCobrancaAsync(Guid id, CancellationToken ct = default)
    {
        var cobranca = await servico.BuscarCobrancaAsync(id, ct);
        if (cobranca is null)
            return Result<CobrancaParceiroResponse>.Falha($"Cobranca {id} n encontrada.");

        return Result<CobrancaParceiroResponse>.Ok(new CobrancaParceiroResponse(cobranca.Id.ToString(), cobranca.Valor, cobranca.Status));
    }
}

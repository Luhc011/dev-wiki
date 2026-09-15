using DevWiki.Microservices.Bff.Interfaces;
using DevWiki.Microservices.Bff.Models;
using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Bff;

public sealed class BffMobile(IServicoCobranca servico)
{
    public async Task<Result<CobrancaMobileResponse>> BuscarCobrancaAsync(Guid id, CancellationToken ct = default)
    {
        var cobranca = await servico.BuscarCobrancaAsync(id, ct);
        if (cobranca is null)
            return Result<CobrancaMobileResponse>.Falha($"Cobranca {id} n encontrada");

        return Result<CobrancaMobileResponse>.Ok(new CobrancaMobileResponse(id, cobranca.Valor, cobranca.Status));
    }
}
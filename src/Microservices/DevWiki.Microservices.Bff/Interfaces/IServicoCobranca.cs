using DevWiki.Microservices.Bff.Models;

namespace DevWiki.Microservices.Bff.Interfaces;

public interface IServicoCobranca
{
    Task<CobrancaDetalhe?> BuscarCobrancaAsync(Guid id, CancellationToken ct = default);
}
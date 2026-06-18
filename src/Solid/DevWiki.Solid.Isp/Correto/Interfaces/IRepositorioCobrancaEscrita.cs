using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Isp.Correto.Interfaces;

public interface IRepositorioCobrancaEscrita
{
    Task SalvarAsync(Cobranca cobranca, StatusPagamento status);
}

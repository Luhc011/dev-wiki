using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Dip.Correto.Interfaces;

public interface IRepositorioPagamento
{
    Task SalvarAsync(ResultadoPagamento resultado);

    Task<ResultadoPagamento?> BuscarAsync(string idTransacao);
}
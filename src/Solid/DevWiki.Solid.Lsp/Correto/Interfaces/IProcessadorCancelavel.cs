namespace DevWiki.Solid.Lsp.Correto.Interfaces;

public interface IProcessadorCancelavel : IProcessadorPagamento
{
    Task CancelarAsync(string idTransacao);
}
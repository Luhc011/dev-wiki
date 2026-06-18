using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Correto.Interfaces;

public interface IProcessadorComComprovante : IProcessadorPagamento
{
    Task<Comprovante> GerarComprovanteAsync(string idTransacao);
}

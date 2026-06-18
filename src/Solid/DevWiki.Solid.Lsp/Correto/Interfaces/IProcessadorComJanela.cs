namespace DevWiki.Solid.Lsp.Correto.Interfaces;

public interface IProcessadorComJanela : IProcessadorPagamento
{
    bool EstaDisponivel(DateTimeOffset agora);

    string DescricaoJanela { get; }
}

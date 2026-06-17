using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Ocp.Correto.Interfaces;

public interface IFormatadorRelatorio
{
    string Formato { get; }
    string Formartar(RelatorioCobranca relatorio);
}
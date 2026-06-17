using DevWiki.Solid.Ocp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Ocp.Correto;

public class FormatadorCsv : IFormatadorRelatorio
{
    public string Formato => "CSV";
    public required char Separador { get; init; }

    public string Formartar(RelatorioCobranca relatorio)
    {
        var sep = Separador;
        var cabecalho = $"Id{sep}Valor{sep}Descricao{sep}DataVencimento";

        var linhas = relatorio.Cobrancas.Select(c => $"{c.Id}{sep}{c.Valor:F2}{sep}{c.Descricao}{sep}{c.DataVencimento}");

        return $"{cabecalho}\n{string.Join('\n', linhas)}";
    }
}

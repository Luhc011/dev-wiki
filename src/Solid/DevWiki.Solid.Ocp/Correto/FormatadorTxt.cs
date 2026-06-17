using DevWiki.Solid.Ocp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Ocp.Correto;

public class FormatadorTxt : IFormatadorRelatorio
{
    public string Formato => "TXT";

    public string Formartar(RelatorioCobranca relatorio)
    {
        var linhas = relatorio.Cobrancas.Select(c => $"  {c.Id} | R$ {c.Valor:F2} | {c.Descricao} | Vence: {c.DataVencimento}");

        return $"""
            relatorio periodo: {relatorio.Periodo:yyyy-MM-dd}
            total processado: R$ {relatorio.TotalProcessado:F2}
            registros: {relatorio.Cobrancas.Count}
            """;
    }
}
using DevWiki.Solid.Ocp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;
using System.Globalization;
using System.Text;

namespace DevWiki.Solid.Ocp.Correto;

public class FormatadorJson : IFormatadorRelatorio
{
    public string Formato => "JSON";

    public string Formartar(RelatorioCobranca relatorio)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine("{");
        sb.AppendLine($"  \"periodo\": \"{relatorio.Periodo:yyyy-MM-dd}\",");
        sb.AppendLine($"  \"totalProcessado\": {relatorio.TotalProcessado.ToString("F2", inv)},");
        sb.AppendLine("  \"cobrancas\": [");

        var linhas = relatorio.Cobrancas.Select(c =>
            $"    {{ \"id\": \"{c.Id}\", \"valor\": {c.Valor.ToString("F2", inv)}, " +
            $"\"descricao\": \"{c.Descricao}\", \"dataVencimento\": \"{c.DataVencimento:yyyy-MM-dd}\" }}");

        sb.AppendLine(string.Join(",\n", linhas));
        sb.AppendLine("  ]");
        sb.Append('}');

        return sb.ToString();
    }
}

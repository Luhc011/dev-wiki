using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Ocp.Violacao;

public class GeradorRelatorio
{
    public string Gerar(RelatorioCobranca relatorio, string formato) => formato switch
    {
        "TXT" => GerarTxt(relatorio),
        "CSV" => GerarCsv(relatorio),
        _ => throw new ArgumentException($"formato {formato} n suportado meu nobre")
    };

    private static string GerarTxt(RelatorioCobranca relatorio)
    {
        var linhas = relatorio.Cobrancas.Select(c => $"  {c.Id} | R$ {c.Valor:F2} | {c.Descricao} | Vence: {c.DataVencimento}");

        return $"""
                relatorio - periodo: {relatorio.Periodo}
                total processado: R$ {relatorio.TotalProcessado:F2}

                {string.Join(Environment.NewLine, linhas)}
            """;
    }

    private static string GerarCsv(RelatorioCobranca relatorio)
    {
        var linhas = relatorio.Cobrancas.Select(c => $"{c.Id};{c.Valor};{c.Descricao};{c.DataVencimento}");

        return $"Id,Valor,Descricao,DataVencimento\n{string.Join('\n', linhas)}";
    }
}
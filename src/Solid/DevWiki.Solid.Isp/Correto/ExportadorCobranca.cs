using DevWiki.Solid.Isp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;
using System.Text;

namespace DevWiki.Solid.Isp.Correto;

public class ExportadorCobranca(IRepositorioCobrancaLeitura repositorio) : IExportadorCobranca
{
    public required char Separador { get; init; }

    public async Task<string> ExportarCsvAsync()
    {
        var cobrancas = await repositorio.ListarPorStatusAsync(StatusPagamento.Pendente);
        var sb = new StringBuilder();

        sb.AppendLine($"Id{Separador}Valor{Separador}Descricao{Separador}DataVencimento");

        foreach (var c in cobrancas)
            sb.AppendLine($"{c.Id}{Separador}{c.Valor}{Separador}{c.Descricao}{Separador}{c.DataVencimento:yyyy-MM-dd}");

        return sb.ToString();
    }
}

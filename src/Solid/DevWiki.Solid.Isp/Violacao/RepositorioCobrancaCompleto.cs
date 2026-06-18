using DevWiki.Solid.Isp.Violacao.Interfaces;
using DevWiki.Solid.Shared.Domain;
using System.Text;

namespace DevWiki.Solid.Isp.Violacao;

public class RepositorioCobrancaCompleto : IRepositorioCobranca
{
    private readonly Dictionary<string, Cobranca> _dados = [];

    public Task<Cobranca?> BuscarPorIdAsync(string id)
    {
        _dados.TryGetValue(id, out var cobranca);
        return Task.FromResult(cobranca);
    }

    public Task<IReadOnlyList<Cobranca>> ListarPorStatusAsync(StatusPagamento status)
    {
        IReadOnlyList<Cobranca> resultado = [.. _dados.Values];
        return Task.FromResult(resultado);
    }

    public Task SalvarAsync(Cobranca cobranca)
    {
        _dados[cobranca.Id] = cobranca;
        return Task.CompletedTask;
    }

    public Task<RelatorioCobranca> GerarRelatorioAsync(DateOnly periodo)
    {
        var cobrancas = _dados.Values.ToList();
        var total = cobrancas.Sum(c => c.Valor);
        var relatorio = new RelatorioCobranca(periodo, [.. cobrancas], total);
        return Task.FromResult(relatorio);
    }

    public Task<string> ExportarCsvAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Valor,Descricao,DataVencimento");

        foreach (var c in _dados.Values)
            sb.AppendLine($"{c.Id},{c.Valor},{c.Descricao},{c.DataVencimento:yyyy-MM-dd}");

        return Task.FromResult(sb.ToString());
    }

    public Task EnviarNotificacoesPendentesAsync()
    {
        Console.WriteLine($"[notificacao] {_dados.Count} cobrancas verifi");
        return Task.CompletedTask;
    }
}

using DevWiki.Solid.Ocp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Ocp.Correto;

public class ServicoRelatorio(IEnumerable<IFormatadorRelatorio> formatadores)
{
    private readonly Dictionary<string, IFormatadorRelatorio> _mapa = formatadores.ToDictionary(f => f.Formato);

    public string Gerar(RelatorioCobranca relatorio, string formato)
    {
        if (!_mapa.TryGetValue(formato, out var formatador))
        {
            var disponiveis = string.Join(", ", [.. _mapa.Keys]);
            throw new ArgumentException($"formato {formato} nao registrado, disponiveis: {disponiveis}", nameof(formato));
        }

        return formatador.Formartar(relatorio);
    }
}
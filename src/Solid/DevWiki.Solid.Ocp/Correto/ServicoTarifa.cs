using DevWiki.Solid.Ocp.Correto.Interfaces;

namespace DevWiki.Solid.Ocp.Correto;

public class ServicoTarifa(IEnumerable<ICalculadoraTarifa> calculadoras)
{
    private readonly Dictionary<string, ICalculadoraTarifa> _mapa = calculadoras.ToDictionary(c => c.TipoPagamento);

    public decimal Calcular(string tipoPagamento, decimal valor)
    {
        if (!_mapa.TryGetValue(tipoPagamento, out var calculadora))
        {
            var disponiveis = string.Join(", ", [.. _mapa.Keys]);
            throw new ArgumentException($"tipo {tipoPagamento} nao registrado, disponiveis {disponiveis}", nameof(tipoPagamento));
        }

        return calculadora.Calcular(valor);
    }
}

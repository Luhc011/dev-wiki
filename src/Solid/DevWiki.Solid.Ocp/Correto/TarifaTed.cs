using DevWiki.Solid.Ocp.Correto.Interfaces;

namespace DevWiki.Solid.Ocp.Correto;

public class TarifaTed : ICalculadoraTarifa
{
    public string TipoPagamento => "TED";
    public decimal TarifaMinima
    {
        get => field;
        set => field = value >= 0m
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "tarifiiiis min n pode ser negativa man");
    }

    public decimal Calcular(decimal valor) => Math.Max(TarifaMinima, valor * 0.002m);
}
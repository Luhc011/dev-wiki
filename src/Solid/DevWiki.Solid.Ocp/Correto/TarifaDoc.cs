using DevWiki.Solid.Ocp.Correto.Interfaces;

namespace DevWiki.Solid.Ocp.Correto;

public class TarifaDoc : ICalculadoraTarifa
{
    public string TipoPagamento => "DOC";
    public decimal Calcular(decimal valor) => Math.Max(8.00m, valor * 0.003m);
}

using DevWiki.Solid.Ocp.Correto.Interfaces;

namespace DevWiki.Solid.Ocp.Correto;

public class TarifaPix : ICalculadoraTarifa
{
    public string TipoPagamento => "PIX";
    public decimal Calcular(decimal valor) => 0m;
}

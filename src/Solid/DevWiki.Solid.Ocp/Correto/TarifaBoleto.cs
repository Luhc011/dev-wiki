using DevWiki.Solid.Ocp.Correto.Interfaces;

namespace DevWiki.Solid.Ocp.Correto;

public class TarifaBoleto : ICalculadoraTarifa
{
    public string TipoPagamento => "Boleto";
    public decimal Calcular(decimal valor) => 2.50m;
}

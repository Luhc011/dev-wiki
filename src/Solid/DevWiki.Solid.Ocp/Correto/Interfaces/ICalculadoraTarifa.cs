namespace DevWiki.Solid.Ocp.Correto.Interfaces;

public interface ICalculadoraTarifa
{
    string TipoPagamento { get; }
    decimal Calcular(decimal valor);
}
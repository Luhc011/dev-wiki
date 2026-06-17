namespace DevWiki.Solid.Ocp.Violacao;

public class CalculadoraTarifa
{
    public decimal Calcular(string tipoPagamento, decimal valor) => tipoPagamento switch
    {
        "PIX" => valor * 0.98m,
        "TED" => valor * 0.97m,
        "Boleto" => valor * 0.95m,
        _ => throw new NotImplementedException()
    };
}
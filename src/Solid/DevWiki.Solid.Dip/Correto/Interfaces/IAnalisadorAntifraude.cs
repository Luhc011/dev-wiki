namespace DevWiki.Solid.Dip.Correto.Interfaces;

public interface IAnalisadorAntifraude
{
    Task<bool> AnalisarRiscoAsync(string identificador, decimal valor);
}
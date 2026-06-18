namespace DevWiki.Solid.Isp.Correto.Interfaces;

public interface IContaSacavel : IContaBancaria
{
    Task SacarAsync(decimal valor);
}

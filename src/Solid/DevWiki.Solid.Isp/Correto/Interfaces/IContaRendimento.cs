namespace DevWiki.Solid.Isp.Correto.Interfaces;

public interface IContaRendimento : IContaBancaria
{
    Task AplicarRendimentoAsync(decimal taxa);
    Task ResgatarAsync(decimal valor, DateOnly dataResgate);
}

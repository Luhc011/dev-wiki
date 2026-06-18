namespace DevWiki.Solid.Isp.Correto.Interfaces;

public interface IContaBancaria
{
    Task<decimal> ConsultarSaldoAsync();

    Task DepositarAsync(decimal valor);
}
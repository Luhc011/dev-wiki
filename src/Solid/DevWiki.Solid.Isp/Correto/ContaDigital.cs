using DevWiki.Solid.Isp.Correto.Interfaces;

namespace DevWiki.Solid.Isp.Correto;

public class ContaDigital(decimal saldoInicial = 0m) : IContaSacavel, IContaTransferivel
{
    private decimal _saldo = saldoInicial;

    public Task<decimal> ConsultarSaldoAsync() => Task.FromResult(_saldo);

    public Task DepositarAsync(decimal valor)
    {
        _saldo += valor;
        return Task.CompletedTask;
    }

    public Task SacarAsync(decimal valor)
    {
        if (valor > _saldo)
            throw new InvalidOperationException("saldo insuficiente");

        _saldo -= valor;
        return Task.CompletedTask;
    }

    public Task TransferirAsync(decimal valor, string contaDestino)
    {
        if (valor > _saldo)
            throw new InvalidOperationException("saldo insuficiente para transf");

        _saldo -= valor;
        return Task.CompletedTask;
    }
}

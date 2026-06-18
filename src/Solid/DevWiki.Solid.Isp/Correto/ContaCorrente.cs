using DevWiki.Solid.Isp.Correto.Interfaces;

namespace DevWiki.Solid.Isp.Correto;

public class ContaCorrente(decimal saldoInicial = 0m) : IContaSacavel, IContaTransferivel, IContaComCartao
{
    private decimal _saldo = saldoInicial;
    private readonly List<string> _cartoes = [];

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

    public Task<string> EmitirCartaoAsync()
    {
        var numero = $"4111-{Random.Shared.Next(1000, 9999)}-{Random.Shared.Next(1000, 9999)}-{Random.Shared.Next(1000, 9999)}";
        _cartoes.Add(numero);
        return Task.FromResult(numero);
    }

    public Task BloquearCartaoAsync(string numeroCartao)
    {
        _cartoes.Remove(numeroCartao);
        return Task.CompletedTask;
    }
}
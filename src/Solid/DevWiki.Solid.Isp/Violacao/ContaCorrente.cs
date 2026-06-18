using DevWiki.Solid.Isp.Violacao.Interfaces;

namespace DevWiki.Solid.Isp.Violacao;

public class ContaCorrente(decimal saldoInicial = 0m) : IContaBancariaViolacao
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

    public Task AplicarRendimentoAsync(decimal taxa)
        => throw new NotSupportedException("conta corrente n possui rendimento aut");

    public Task ResgatarAsync(decimal valor, DateOnly dataResgate)
        => throw new NotSupportedException("conta corrente n possui resgate programado");

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
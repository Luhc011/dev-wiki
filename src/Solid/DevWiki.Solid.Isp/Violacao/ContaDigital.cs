using DevWiki.Solid.Isp.Violacao.Interfaces;

namespace DevWiki.Solid.Isp.Violacao;

public class ContaDigital(decimal saldoInicial = 0m) : IContaBancaria
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
            throw new InvalidOperationException("saldo insuficiente para transferencia");
        _saldo -= valor;
        return Task.CompletedTask;
    }

    public Task AplicarRendimentoAsync(decimal taxa)
        => throw new NotSupportedException("conta digital n possui rendimento aut");

    public Task ResgatarAsync(decimal valor, DateOnly dataResgate)
        => throw new NotSupportedException("conta digital n possui resgate programado");

    public Task<string> EmitirCartaoAsync()
        => throw new NotSupportedException("conta digital n emite cartao fsico");

    public Task BloquearCartaoAsync(string numeroCartao)
        => throw new NotSupportedException("conta digital n possui cartao para bloq");
}

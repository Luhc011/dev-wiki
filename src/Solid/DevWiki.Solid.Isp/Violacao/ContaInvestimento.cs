using DevWiki.Solid.Isp.Violacao.Interfaces;

namespace DevWiki.Solid.Isp.Violacao;

public class ContaInvestimento(decimal saldoInicial = 0m) : IContaBancaria
{
    private decimal _saldo = saldoInicial;

    public Task<decimal> ConsultarSaldoAsync() => Task.FromResult(_saldo);

    public Task DepositarAsync(decimal valor)
    {
        _saldo += valor;
        return Task.CompletedTask;
    }

    public Task SacarAsync(decimal valor)
        => throw new NotSupportedException("conta investimento n permite saque direto");

    public Task TransferirAsync(decimal valor, string contaDestino)
        => throw new NotSupportedException("conta investimento n suporta transf");

    public Task AplicarRendimentoAsync(decimal taxa)
    {
        _saldo += _saldo * taxa;
        return Task.CompletedTask;
    }

    public Task ResgatarAsync(decimal valor, DateOnly dataResgate)
    {
        if (dataResgate < DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("data de resgate n pode ser no passado", nameof(dataResgate));

        return Task.CompletedTask;
    }

    public Task<string> EmitirCartaoAsync()
        => throw new NotSupportedException("conta investimento n emite cartao");

    public Task BloquearCartaoAsync(string numeroCartao)
        => throw new NotSupportedException("conta investimento n possui cartao para bloquear");
}
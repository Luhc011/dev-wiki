using DevWiki.Solid.Isp.Correto.Interfaces;

namespace DevWiki.Solid.Isp.Correto;

public class ContaInvestimento(decimal saldoInicial = 0m) : IContaRendimento
{
    private decimal _saldo = saldoInicial;
    public decimal TaxaMinimaRendimento
    {
        get;
        init => field = value >= 0m
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "taxa min n pode ser negativa");
    } = 0m;

    public Task<decimal> ConsultarSaldoAsync() => Task.FromResult(_saldo);

    public Task DepositarAsync(decimal valor)
    {
        _saldo += valor;
        return Task.CompletedTask;
    }

    public Task AplicarRendimentoAsync(decimal taxa)
    {
        var taxaAplicada = taxa >= TaxaMinimaRendimento ? taxa : TaxaMinimaRendimento;
        _saldo += _saldo * taxaAplicada;
        return Task.CompletedTask;
    }

    public Task ResgatarAsync(decimal valor, DateOnly dataResgate)
    {
        if (dataResgate < DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("data de resgate n pode ser no passado", nameof(dataResgate));

        return Task.CompletedTask;
    }
}

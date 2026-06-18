namespace DevWiki.Solid.Isp.Violacao.Interfaces;

public interface IContaBancaria
{
    Task<decimal> ConsultarSaldoAsync();
    Task DepositarAsync(decimal valor);
    Task SacarAsync(decimal valor);
    Task TransferirAsync(decimal valor, string contaDestino);
    Task AplicarRendimentoAsync(decimal taxa);
    Task ResgatarAsync(decimal valor, DateOnly dataResgate);
    Task<string> EmitirCartaoAsync();
    Task BloquearCartaoAsync(string numeroCartao);
}
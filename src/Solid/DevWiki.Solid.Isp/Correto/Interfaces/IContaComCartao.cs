namespace DevWiki.Solid.Isp.Correto.Interfaces;

public interface IContaComCartao : IContaBancaria
{
    Task<string> EmitirCartaoAsync();
    Task BloquearCartaoAsync(string numeroCartao);
}

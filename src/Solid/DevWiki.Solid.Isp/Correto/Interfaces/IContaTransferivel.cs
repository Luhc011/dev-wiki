namespace DevWiki.Solid.Isp.Correto.Interfaces;

public interface IContaTransferivel : IContaBancaria
{
    Task TransferirAsync(decimal valor, string contaDestino);
}

using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Dip.Violacao;

public class OracleRepositorio
{
    private readonly string _connectionString = "fake-connection-string";

    public async Task SalvarAsync(ResultadoPagamento resultado)
    {
        await Task.Delay(100);
        Console.WriteLine(
            $"oracle@{_connectionString}" +
            $"INSERT INTO TB_PAGAMENTOS VALUES ({resultado.IdTransacao}, {resultado.Status})");
    }
}
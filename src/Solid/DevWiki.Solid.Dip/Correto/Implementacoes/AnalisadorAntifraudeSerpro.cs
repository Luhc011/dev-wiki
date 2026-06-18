using DevWiki.Solid.Dip.Correto.Interfaces;

namespace DevWiki.Solid.Dip.Correto.Implementacoes;

public class AnalisadorAntifraudeSerpro : IAnalisadorAntifraude
{
    public int TimeoutMs
    {
        get;
        init => field = value is > 0 and <= 30_000
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "timeout deve estar entre 1ms e 30s");
    } = 5_000;

    public async Task<bool> AnalisarRiscoAsync(string identificador, decimal valor)
    {
        await Task.Delay(20);

        return identificador is not (null or "") && valor is not (<= 0m or > 100_000m);
    }
}
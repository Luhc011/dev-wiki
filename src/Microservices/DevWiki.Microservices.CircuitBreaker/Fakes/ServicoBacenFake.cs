using DevWiki.Microservices.CircuitBreaker.Interfaces;
using DevWiki.Microservices.CircuitBreaker.Models;

namespace DevWiki.Microservices.CircuitBreaker.Fakes;

public sealed class ServicoBacenFake : IServicoBacen
{
    public int ChamadasRecebidas { get; private set; }

    public int FalhasRestantes
    {
        get;
        set => field = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "N pode ser negativo.");
    }

    public ServicoBacenFake(int falhasIniciais = 0) => FalhasRestantes = falhasIniciais;

    public async Task<ResultadoPagamento> ProcessarAsync(Guid cobrancaId, decimal valor, CancellationToken ct = default)
    {
        ChamadasRecebidas++;
        await Task.Delay(1, ct);

        if (FalhasRestantes > 0)
        {
            FalhasRestantes--;
            throw new HttpRequestException("BACEN indisponvel — timeout na conexao.");
        }

        return new ResultadoPagamento(cobrancaId, $"AUTH-{cobrancaId:N}");
    }
}
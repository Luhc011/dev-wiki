using DevWiki.Microservices.Saga.Interfaces;
using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Fakes;

public sealed class ServicoAntifraudeFake : IServicoAntifraude
{
    private readonly HashSet<Guid> _cobrancasRejeitadas = [];

    public int TotalAnalisadas { get; private set; }

    public void AdicionarRejeicao(Guid cobrancaId) => _cobrancasRejeitadas.Add(cobrancaId);

    public async Task<Result<bool>> AnalisarAsync(
        Guid cobrancaId, string cpf, decimal valor, CancellationToken ct = default)
    {
        TotalAnalisadas++;
        await Task.Delay(5, ct);

        return _cobrancasRejeitadas.Contains(cobrancaId)
            ? Result<bool>.Falha("Cobranca rejeitada pelo sistema antifraude.")
            : Result<bool>.Ok(true);
    }
}

using DevWiki.Microservices.Saga.Interfaces;
using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Fakes;

public sealed class ServicoDebitoFake : IServicoDebito
{
    private readonly HashSet<Guid> _cobrancasDebitadas = [];
    private readonly HashSet<Guid> _cobrancasComFalha = [];

    public int TotalDebitadas => _cobrancasDebitadas.Count;
    public int TotalCancelamentos { get; private set; }

    public void ForcarFalha(Guid cobrancaId) => _cobrancasComFalha.Add(cobrancaId);

    public bool EstaDebitada(Guid cobrancaId) => _cobrancasDebitadas.Contains(cobrancaId);

    public async Task<Result<bool>> DebitarAsync(Guid cobrancaId, decimal valor, CancellationToken ct = default)
    {
        await Task.Delay(5, ct);

        if (_cobrancasComFalha.Contains(cobrancaId))
            return Result<bool>.Falha("Falha ao processar debito: saldo insuficiente.");

        _cobrancasDebitadas.Add(cobrancaId);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CancelarDebitoAsync(Guid cobrancaId, CancellationToken ct = default)
    {
        await Task.Delay(5, ct);

        _cobrancasDebitadas.Remove(cobrancaId);
        TotalCancelamentos++;
        return Result<bool>.Ok(true);
    }
}

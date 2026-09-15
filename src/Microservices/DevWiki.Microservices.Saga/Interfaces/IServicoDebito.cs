using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Interfaces;

public interface IServicoDebito
{
    Task<Result<bool>> DebitarAsync(
        Guid cobrancaId, decimal valor, CancellationToken ct = default);

    Task<Result<bool>> CancelarDebitoAsync(Guid cobrancaId, CancellationToken ct = default);
}

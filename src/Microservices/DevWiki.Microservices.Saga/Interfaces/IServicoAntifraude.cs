using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Interfaces;

public interface IServicoAntifraude
{
    Task<Result<bool>> AnalisarAsync(Guid cobrancaId, string cpf, decimal valor, CancellationToken ct = default);
}

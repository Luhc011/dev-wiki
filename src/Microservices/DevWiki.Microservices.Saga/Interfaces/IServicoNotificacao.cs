using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Interfaces;

public interface IServicoNotificacao
{
    Task<Result<bool>> NotificarAsync(Guid cobrancaId, string destinatario, string mensagem, CancellationToken ct = default);
}
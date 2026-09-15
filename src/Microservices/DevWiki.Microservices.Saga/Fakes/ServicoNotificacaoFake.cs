using DevWiki.Microservices.Saga.Interfaces;
using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Fakes;

public sealed class ServicoNotificacaoFake : IServicoNotificacao
{
    private readonly HashSet<Guid> _cobrancasComFalha = [];
    private readonly List<string> _notificacoesEnviadas = [];

    public IReadOnlyList<string> NotificacoesEnviadas => [.. _notificacoesEnviadas];
    public int TotalNotificacoes => _notificacoesEnviadas.Count;

    public void ForcarFalha(Guid cobrancaId) => _cobrancasComFalha.Add(cobrancaId);

    public async Task<Result<bool>> NotificarAsync(Guid cobrancaId,
                                                   string destinatario,
                                                   string mensagem,
                                                   CancellationToken ct = default)
    {
        await Task.Delay(3, ct);

        if (_cobrancasComFalha.Contains(cobrancaId))
            return Result<bool>.Falha("Falha ao enviar notificacao: servidor de e-mail indisponvel.");

        _notificacoesEnviadas.Add($"[{destinatario}]: {mensagem}");
        return Result<bool>.Ok(true);
    }
}
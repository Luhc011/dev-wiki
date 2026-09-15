using DevWiki.Microservices.Saga.Interfaces;
using DevWiki.Microservices.Shared.Domain;
using DevWiki.Microservices.Shared.SharedKernel;

namespace DevWiki.Microservices.Saga.Orchestration;

public sealed class SagaOrchestrator(IServicoAntifraude antifraude, IServicoDebito debito, IServicoNotificacao notificacao)
{
    private readonly Dictionary<Guid, SagaState> _sagas = [];

    public IReadOnlyDictionary<Guid, SagaState> Sagas => _sagas;

    public async Task<Result<SagaState>> ExecutarAsync(
        Guid cobrancaId, string cpf, decimal valor, CancellationToken ct = default)
    {
        var state = new SagaState
        {
            CobrancaId = cobrancaId,
            CpfDevedor = cpf,
            Valor = valor
        };
        _sagas[state.SagaId] = state;

        state.StatusAntifraude = StepStatus.EmAndamento;
        var resultadoAntifraude = await antifraude.AnalisarAsync(cobrancaId, cpf, valor, ct);
        if (!resultadoAntifraude.Sucesso)
        {
            state.StatusAntifraude = StepStatus.Falhou;
            state.MotivoFalha = resultadoAntifraude.MensagemErro;
            state.FinalizadoEm = DateTimeOffset.UtcNow;
            return Result<SagaState>.Falha($"Antifraude rejeitou: {resultadoAntifraude.MensagemErro}");
        }
        state.StatusAntifraude = StepStatus.Concluido;

        state.StatusDebito = StepStatus.EmAndamento;
        var resultadoDebito = await debito.DebitarAsync(cobrancaId, valor, ct);
        if (!resultadoDebito.Sucesso)
        {
            state.StatusDebito = StepStatus.Falhou;
            state.MotivoFalha = resultadoDebito.MensagemErro;
            state.FinalizadoEm = DateTimeOffset.UtcNow;

            return Result<SagaState>.Falha($"Debito falhou: {resultadoDebito.MensagemErro}");
        }
        state.StatusDebito = StepStatus.Concluido;

        state.StatusNotificacao = StepStatus.EmAndamento;
        var destinatario = $"cliente-{cobrancaId:N}";
        var mensagem = $"Cobranca de R$ {valor:F2} aprovada.";
        var resultadoNotificacao = await notificacao.NotificarAsync(cobrancaId, destinatario, mensagem, ct);
        if (!resultadoNotificacao.Sucesso)
        {
            state.StatusNotificacao = StepStatus.Falhou;
            state.MotivoFalha = resultadoNotificacao.MensagemErro;

            await CompensarAsync(state, ct);

            state.FinalizadoEm = DateTimeOffset.UtcNow;
            return Result<SagaState>.Falha($"Notificacao falhou, debito compensado: {resultadoNotificacao.MensagemErro}");
        }
        state.StatusNotificacao = StepStatus.Concluido;
        state.FinalizadoEm = DateTimeOffset.UtcNow;

        return Result<SagaState>.Ok(state);
    }

    public Task<SagaState?> BuscarEstadoAsync(Guid sagaId)
    {
        _sagas.TryGetValue(sagaId, out var state);
        return Task.FromResult(state);
    }

    private async Task CompensarAsync(SagaState state, CancellationToken ct)
    {
        if (state.StatusDebito is StepStatus.Concluido)
        {
            await debito.CancelarDebitoAsync(state.CobrancaId, ct);
            state.StatusDebito = StepStatus.Compensado;
        }
    }
}
using DevWiki.Microservices.Shared.Domain;

namespace DevWiki.Microservices.Saga.Orchestration;

public sealed class SagaState
{
    public Guid SagaId { get; } = Guid.NewGuid();
    public required Guid CobrancaId { get; init; }
    public required string CpfDevedor { get; init; }
    public required decimal Valor { get; init; }

    public StepStatus StatusAntifraude { get; set; } = StepStatus.NaoIniciado;
    public StepStatus StatusDebito { get; set; } = StepStatus.NaoIniciado;
    public StepStatus StatusNotificacao { get; set; } = StepStatus.NaoIniciado;

    public string? MotivoFalha { get; set; }
    public DateTimeOffset IniciadoEm { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinalizadoEm { get; set; }

    public bool Concluida =>
        StatusAntifraude is StepStatus.Concluido &&
        StatusDebito is StepStatus.Concluido &&
        StatusNotificacao is StepStatus.Concluido;

    public bool Falhou =>
        StatusAntifraude is StepStatus.Falhou ||
        StatusDebito is StepStatus.Falhou ||
        StatusNotificacao is StepStatus.Falhou;

    public bool NecessitaCompensacao => StatusDebito is StepStatus.Concluido &&
        StatusNotificacao is StepStatus.Falhou;
}
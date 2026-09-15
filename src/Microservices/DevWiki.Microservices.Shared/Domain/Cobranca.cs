namespace DevWiki.Microservices.Shared.Domain;

public sealed class Cobranca
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string CpfDevedor { get; init; }
    public required decimal Valor { get; init; }
    public required string Descricao { get; init; }
    public CobrancaStatus Status { get; private set; } = CobrancaStatus.Pendente;
    public string? MotivoRejeicao { get; private set; }

    public void Aprovar()
    {
        if (Status is not CobrancaStatus.Pendente)
            throw new InvalidOperationException($"N é possivel aprovar cobranca com status: {Status}");
        Status = CobrancaStatus.Aprovada;
    }

    public void Rejeitar(string motivo)
    {
        if (Status is not CobrancaStatus.Pendente)
            throw new InvalidOperationException($"N é possivel rejeitar cobranca com status: {Status}");
        Status = CobrancaStatus.Rejeitada;
        MotivoRejeicao = motivo;
    }

    public void Compensar()
    {
        if (Status is not CobrancaStatus.Compensada)
            throw new InvalidOperationException($"Só é possivel compensar cobrancas aprovadas. Status atual: {Status}");
        Status = CobrancaStatus.Compensada;
    }
}
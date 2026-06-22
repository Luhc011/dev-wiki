using DevWiki.CleanArch.Domain.Enums;
using DevWiki.CleanArch.Domain.SharedKernel;
using DevWiki.CleanArch.Domain.ValueObjects;

namespace DevWiki.CleanArch.Domain.Entities;

public sealed class Cobranca : Entity
{
    private readonly List<Pagamento> _pagamentos = [];

    private Cobranca(CobrancaId id, Cpf cpfDevedor, Valor valorTotal, string descricao, DateOnly dataVencimento) : base(id.Valor)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("descricao n pode ser vazia", nameof(descricao));

        CobrancaId = id;
        CpfDevedor = cpfDevedor;
        ValorTotal = valorTotal;
        Descricao = descricao;
        DataVencimento = dataVencimento;
        Status = StatusCobranca.Pendente;
    }

    public CobrancaId CobrancaId { get; }
    public Cpf CpfDevedor { get; }
    public Valor ValorTotal { get; }
    public string Descricao { get; }
    public DateOnly DataVencimento { get; }
    public StatusCobranca Status { get; private set; }
    public IReadOnlyList<Pagamento> Pagamentos => [.. _pagamentos];

    public static Cobranca Criar(Cpf cpfDevedor, Valor valorTotal, string descricao, DateOnly dataVencimento)
        => new(CobrancaId.Novo(), cpfDevedor, valorTotal, descricao, dataVencimento);

    public Result<Pagamento> RegistrarPagamento(Valor valorPago, DateTimeOffset dataHora)
    {
        if (Status is StatusCobranca.Paga)
            return Result<Pagamento>.Falha("cobranca ja foi paga");
        if (Status is StatusCobranca.Cancelada)
            return Result<Pagamento>.Falha("cobranca cancelada");
        if (valorPago.Quantia < ValorTotal.Quantia)
            return Result<Pagamento>.Falha($"valor pago ({valorPago.Quantia}) n pode ser menor que o valor total ({ValorTotal.Quantia})");

        var pagamento = Pagamento.Criar(CobrancaId, valorPago, dataHora);
        _pagamentos.Add(pagamento);
        Status = StatusCobranca.Paga;
        return Result<Pagamento>.Ok(pagamento);
    }

    public Result<Cobranca> Cancelar()
    {
        if (Status is StatusCobranca.Paga)
            return Result<Cobranca>.Falha("cobranca ja foi paga");
        if (Status is StatusCobranca.Cancelada)
            return Result<Cobranca>.Falha("cobranca ja foi cancelada");

        Status = StatusCobranca.Cancelada;
        return Result<Cobranca>.Ok(this);
    }
}
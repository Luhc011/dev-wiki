using DevWiki.CleanArch.Domain.SharedKernel;
using DevWiki.CleanArch.Domain.ValueObjects;

namespace DevWiki.CleanArch.Domain.Entities;

public sealed class Pagamento : Entity
{
    internal Pagamento(CobrancaId cobrancaId, Valor valor, DateTimeOffset dataHora)
        : base(Guid.NewGuid())
    {
        CobrancaId = cobrancaId;
        Valor = valor;
        DataHora = dataHora;
    }

    public CobrancaId CobrancaId { get; }
    public Valor Valor { get; }
    public DateTimeOffset DataHora { get; }

    internal static Pagamento Criar(CobrancaId cobrancaId, Valor valor, DateTimeOffset dataHora) =>
        new(cobrancaId, valor, dataHora);
}
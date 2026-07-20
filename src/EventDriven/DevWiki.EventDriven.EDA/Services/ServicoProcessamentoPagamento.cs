using DevWiki.EventDriven.EDA.Events;
using DevWiki.EventDriven.EDA.Interfaces;
using DevWiki.EventDriven.Shared.SharedKernel;

namespace DevWiki.EventDriven.EDA.Services;

public sealed class ServicoProcessamentoPagamento(IEventBus eventBus)
{
    public async Task<Result<Guid>> ProcessarAsync(string cpfPagador,
        decimal valor, string tipoPagamento, CancellationToken ct = default)
    {
        if (valor <= 0)
            return Result<Guid>.Falha("valor do pagamento deve ser positivo");

        var cobrancaId = Guid.NewGuid();

        var evento = new PagamentoProcessadoEvento
        {
            CobrancaId = cobrancaId,
            CpfPagador = cpfPagador,
            Valor = valor,
            TipoPagamento = tipoPagamento
        };

        await eventBus.PublicarAsync(evento, ct);

        return Result<Guid>.Ok(cobrancaId);
    }
}
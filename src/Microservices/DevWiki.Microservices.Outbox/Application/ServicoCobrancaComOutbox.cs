using DevWiki.Microservices.Outbox.Interfaces;
using DevWiki.Microservices.Shared.Domain;

namespace DevWiki.Microservices.Outbox.Application;

public sealed class ServicoCobrancaComOutbox(IOutboxRepositorio outbox)
{
    public async Task<Cobranca> CriarCobrancaAsync(string cpf, decimal valor, CancellationToken ct = default)
    {
        var cobranca = new Cobranca
        {
            CpfDevedor = cpf,
            Valor = valor,
            Descricao = $"Cobrança {DateTimeOffset.UtcNow:dd/MM/yyyy HH:mm}"
        };

        var eventoOutbox = new OutboxEvento
        {
            NomeEvento = "CobrancaCriadaEvento",
            Payload = $"{{\"cobrancaId\":\"{cobranca.Id}\",\"cpf\":\"{cpf}\",\"valor\":{valor}}}"
        };
        await outbox.SalvarEventoPendenteAsync(eventoOutbox, ct);

        return cobranca;
    }
}
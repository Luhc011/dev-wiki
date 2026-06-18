using DevWiki.Solid.Dip.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Dip.Correto.Implementacoes;

public class GatewayPixBacen : IGatewayPagamento
{
    public required string PrefixoTransacao { get; init; }

    public async Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
    {
        ArgumentNullException.ThrowIfNull(solicitacao, nameof(solicitacao));

        await Task.Delay(30);

        var idTransacao = $"{PrefixoTransacao}-{Guid.NewGuid():N}";

        return new ResultadoPagamento(
            idTransacao,
            StatusPagamento.Processado,
            $"PIX via BACEN — R$ {solicitacao.Valor:F2}");
    }
}
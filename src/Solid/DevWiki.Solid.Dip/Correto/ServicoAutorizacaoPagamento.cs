using DevWiki.Solid.Dip.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Dip.Correto;

public class ServicoAutorizacaoPagamento(IAnalisadorAntifraude antiFraude,
                                         IGatewayPagamento gateway,
                                         IRepositorioPagamento repo,
                                         INotificadorPagamento notificador)
{
    public async Task<ResultadoPagamento> AutorizarAsync(SolicitacaoPagamento solicitacao)
    {
        ArgumentNullException.ThrowIfNull(solicitacao, nameof(solicitacao));

        var aprovado = await antiFraude.AnalisarRiscoAsync(solicitacao.IdSolicitacao, solicitacao.Valor);

        if (!aprovado)
            return new ResultadoPagamento(null, StatusPagamento.Cancelado, "reprovado pela analise de risco");

        var resultado = await gateway.ProcessarAsync(solicitacao);

        await repo.SalvarAsync(resultado);

        var mensagem = resultado.Status switch
        {
            StatusPagamento.Processado => $"pagemanto {resultado.IdTransacao} aprovado - r$ {solicitacao.Valor:F2}",
            StatusPagamento.AguardandoCompensacao => $"pagamento {resultado.IdTransacao} aguardando compensacao - r$ {solicitacao.Valor:F2}",
            _ => $"pagamento com status {resultado.Status} - verifiq o extrato"
        };

        await notificador.NotificarAsync(solicitacao.BancoOrigem, mensagem);

        return resultado;
    }
}
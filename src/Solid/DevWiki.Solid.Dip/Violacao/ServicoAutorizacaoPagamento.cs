using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Dip.Violacao;

public class ServicoAutorizacaoPagamento
{
    private readonly SistemaSerpro _antiFraude = new();
    private readonly OracleRepositorio _repo = new();
    private readonly SmtpNotificador _notificador = new();

    public async Task<ResultadoPagamento> AutorizarAsync(SolicitacaoPagamento solicitacao)
    {
        var aprovado = await _antiFraude.AnalisarRiscoAsync(solicitacao.IdSolicitacao, solicitacao.Valor);

        if (!aprovado)
            return new ResultadoPagamento(null, StatusPagamento.Cancelado, "reprovado serpro");

        var resultado = new ResultadoPagamento(
            Guid.NewGuid().ToString("N"),
            StatusPagamento.Processado,
            $"Pagamento autorizado via Oracle — R$ {solicitacao.Valor:F2}");

        await _repo.SalvarAsync(resultado);
        await _notificador.NotificarAsync(solicitacao.BancoOrigem, $"autorizado: {resultado.IdTransacao}");

        return resultado;
    }
}

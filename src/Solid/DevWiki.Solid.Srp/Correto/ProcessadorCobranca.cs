using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Srp.Correto;

public class ProcessadorCobranca : IProcessadorCobranca
{
    private readonly IValidadorSolicitacao _validador;
    private readonly IRepositorioPagamento _repositorio;
    private readonly INotificacaoService _notificacaoService;
    private readonly IRelatorioService _relatorioService;

    public ProcessadorCobranca(IValidadorSolicitacao validador,
                               IRepositorioPagamento repositorio,
                               INotificacaoService notificacaoService,
                               IRelatorioService relatorioService)
    {
        _validador = validador;
        _repositorio = repositorio;
        _notificacaoService = notificacaoService;
        _relatorioService = relatorioService;
    }

    public async Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
    {
        var validacao = _validador.Validar(solicitacao);

        if (!validacao.IsValido)
            return new ResultadoPagamento(null, StatusPagamento.Cancelado, validacao.MensagemErro);

        await Task.Delay(5);

        var resultado = new ResultadoPagamento(Guid.NewGuid().ToString("N"), StatusPagamento.Processado, "pagamento aprovado");

        await _repositorio.SalvarAsync(resultado);

        await _notificacaoService.NotificarAsync("luk.com.br", $"cbranca processda. tRANSACAO: {resultado.IdTransacao}");

        return resultado;
    }
}
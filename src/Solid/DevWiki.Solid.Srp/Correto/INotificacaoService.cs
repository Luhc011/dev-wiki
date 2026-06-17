using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Srp.Correto;

public interface INotificacaoService
{
    Task NotificarAsync(string destinatario, string mensagem);
}

public interface IProcessadorCobranca
{
    Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao);
}

public interface IRelatorioService
{
    string Gerar(IEnumerable<ResultadoPagamento> resultados);
}

public interface IRepositorioPagamento
{
    Task SalvarAsync(ResultadoPagamento resultado);
    Task<ResultadoPagamento?> BuscarAsync(string idTransacao);
}

public interface IValidadorSolicitacao
{
    ResultadoValidacao Validar(SolicitacaoPagamento solicitacao);
}

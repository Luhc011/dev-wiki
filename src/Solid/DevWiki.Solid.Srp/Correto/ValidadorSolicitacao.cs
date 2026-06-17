using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Srp.Correto;

public class ValidadorSolicitacao : IValidadorSolicitacao
{
    public ResultadoValidacao Validar(SolicitacaoPagamento solicitacao) =>
        solicitacao.Valor > 0
        && !string.IsNullOrEmpty(solicitacao.BancoOrigem)
        && !string.IsNullOrEmpty(solicitacao.Descricao)
            ? new ResultadoValidacao(true, null)
            : new ResultadoValidacao(false, "solicitacao invalida");
}
public record ResultadoValidacao(bool IsValido, string? MensagemErro);
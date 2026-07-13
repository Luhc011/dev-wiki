using DevWiki.Solid.Shared.Domain;
using DevWiki.Solid.Srp.Violacao;
using FluentAssertions;

namespace DevWiki.Solid.Tests._1__SingleResponsibility;

public class SrpViolacaoTests
{
    [Fact(DisplayName = "ServicoCobranca: mudar regras de e-mail afeta a classe inteira")]
    public void ServicoCobranca_ProcessarCobrancaAsync_QuandoMudaRegrasEmail_ClasseInteiraEAfetada()
    {
        var servico = new ServicoCobranca();

        servico.Should().NotBeNull("a classe existe mas agrega responsabilidades demais");
    }

    [Fact(DisplayName = "ServicoCobranca: n é possivel testar processamento sem efeitos colaterais")]
    public async Task ServicoCobranca_ProcessarCobrancaAsync_NaoConsegueTestarProcessamentoSemEfeitos()
    {
        var servico = new ServicoCobranca();
        var solicitacao = new SolicitacaoPagamento(IdSolicitacao: "sol-001",
                                                   Valor: 150m,
                                                   Descricao: "cobranca mensalidad",
                                                   BancoOrigem: "341");

        var resultado = await servico.ProcessarCobrancaAsync(solicitacao);

        resultado.Status
            .Should()
            .Be(StatusPagamento.Processado, because: "para verificar o processamento somos forcados a aceitar todos os efeitos colaterais");
    }

    [Fact(DisplayName = "ServicoCobranca: ValidarSolicitacao é privado e não testável isoladamente")]
    public void ServicoCobranca_ValidarSolicitacao_EMetodoPrivadoNaoTestavel()
    {
        var servico = new ServicoCobranca();

        var metodoPrivado = typeof(ServicoCobranca)
            .GetMethod("ValidarSolicitacao",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        metodoPrivado.Should().NotBeNull(
            because: "o metodo existe, mas o acesso por reflexao em testes é sinal de design problematico");
    }
}
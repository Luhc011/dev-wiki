using DevWiki.Solid.Lsp.Violacao;
using DevWiki.Solid.Shared.Domain;
using FluentAssertions;

namespace DevWiki.Solid.Tests._3__LiskovSubstitution;

public class LspViolacaoTests
{
    private static readonly SolicitacaoPagamento _solicitacaoValida = new("SOL-V-001", 300m, "Mensalidade", "341");

    [Fact(DisplayName = "Boleto.CancelarAsync: deve lancar NotSupportedException (violacao 3)")]
    public async Task CancelarAsync_Boleto_DeveLancarNotSupportedException()
    {
        var boleto = new ProcessadorBoleto();

        var acao = async () => await boleto.CancelarAsync("TXN-BOLETO-001");

        await acao
            .Should()
            .ThrowAsync<NotSupportedException>(because: "ProcessadorBoleto quebra o contrato de CancelarAsync — violacao LSP");
    }

    [Fact(DisplayName = "Boleto.GerarComprovanteAsync: deve retornar null quando n compensado (violacao 2)")]
    public async Task GerarComprovanteAsync_Boleto_QuandoNaoCompensado_DeveRetornarNull()
    {
        var boleto = new ProcessadorBoleto();

        var comprovante = await boleto.GerarComprovanteAsync("TXN-BOLETO-001");

        comprovante
            .Should()
            .BeNull(because: "ProcessadorBoleto enfraquece a pos-condicao: retorna null quando n ha compensacao");
    }

    [Fact(DisplayName = "Boleto.ProcessarAsync: deve retornar resultado com IdTransacao nulo (violacao 1)")]
    public async Task ProcessarAsync_Boleto_DeveRetornarResultadoComIdTransacaoNulo()
    {
        var boleto = new ProcessadorBoleto();

        var resultado = await boleto.ProcessarAsync(_solicitacaoValida);

        //violacao 1: supertipo implica IdTransacao preenchido, boleto retorna null
        resultado.IdTransacao
            .Should()
            .BeNull(because: "ProcessadorBoleto enfraquece a pos-condicao: boleto registrado n tem IdTransacao");

        resultado.Status.Should().Be(StatusPagamento.AguardandoCompensacao);
    }

    [Fact(DisplayName = "Ted.ProcessarAsync: fora do horario bancario deve lancar InvalidOperationException (violacao 4)")]
    public async Task ProcessarAsync_Ted_ForaDoHorarioBancario_DeveLancarInvalidOperationException()
    {
        //injeta relogio com hora fora da janela bancaria (20h de segunda)
        var relogioForaDoHorario = () => new DateTimeOffset(2026, 6, 15, 20, 0, 0, TimeSpan.Zero);
        var ted = new ProcessadorTed(relogioForaDoHorario);

        var acao = async () => await ted.ProcessarAsync(_solicitacaoValida);

        //violacao 4: supertipo aceita qlqr solicitacao valida, TED rejeita por horario
        await acao
            .Should()
            .ThrowAsync<InvalidOperationException>(because: "ProcessadorTed fortalece a pre-condicao ao exigir horario bancario — " +
                     "uma solicitacao valida as 20h é rejeitada aqui mas seria aceita pela base");
    }

    [Fact(DisplayName = "Ted.CancelarAsync: deve lancar NotSupportedException (violacao 5)")]
    public async Task CancelarAsync_Ted_DeveLancarNotSupportedException()
    {
        var ted = new ProcessadorTed();

        var acao = async () => await ted.CancelarAsync("TXN-TED-001");

        //violacao 5: supertipo promete que CancelarAsync funciona; TED lanca excecao
        await acao
            .Should()
            .ThrowAsync<NotSupportedException>(because: "ProcessadorTed quebra o contrato de CancelarAsync — TED liquidada é irrevogavel");
    }

    [Fact(DisplayName = "ServicoViolacao.ProcessarCobrancaAsync: requer condicional de tipo para Boleto no caller")]
    public async Task ProcessarCobrancaAsync_ComBoleto_RequereCondicionalDeTipoNoCaller()
    {
        /*
         * PROBLEMA DE DESIGN documentado por este teste:
         * ServicoCobrancaViolacao.ProcessarCobrancaAsync contem:
         *
         *   if (processador is ProcessadorBoleto) { ... }   // ← linha com condicional de tipo
         *
         * O caller acumulou conhecimento dos subtipos concretos. Cada novo processador
         * que violar o LSP exige mais um bloco if aqui — LSP e OCP violados juntos
         *
         * No design correto (ServicoCobrancaCorreto), n existe nenhum 'is ProcessadorX'
         */

        var servico = new ServicoCobrancaViolacao();
        var boleto = new ProcessadorBoleto();

        var resultado = await servico.ProcessarCobrancaAsync(boleto, _solicitacaoValida);

        //resultado evidencia a quebra de pos-condicao absorvida pelo caller
        resultado.Status.Should().Be(StatusPagamento.AguardandoCompensacao);
        resultado.IdTransacao
            .Should()
            .BeNull(because: "ProcessadorBoleto quebra a pos-condicao, o caller precisou de null check defensivo");
    }

    [Fact(DisplayName = "ServicoViolacao.CancelarAsync: precisa absorver NotSupportedException como comportamento normal")]
    public async Task CancelarAsync_ServicoViolacao_PrecisaAbsorverNotSupportedExceptionComoNormal()
    {
        /*
         * ANTI-PATTERN documentado por este teste:
         * ServicoCobrancaViolacao.CancelarAsync contem:
         *
         *   catch (NotSupportedException) { Console.WriteLine(...) }
         *
         * Absorver NotSupportedException como "normal" é o sinal mais claro de LSP violado
         * O sistema funcionou, mas silenciou uma falha de contrato.
         *
         * No design correto (ServicoCobrancaCorreto.CancelarAsync), o parametro é
         * IProcessadorCancelavel — o compilador impede que Boleto chegue aqui.
         */

        var servico = new ServicoCobrancaViolacao();
        var boleto = new ProcessadorBoleto();

        //Nao lanca: ServicoCobrancaViolacao absorve a NotSupportedException
        var acao = async () => await servico.CancelarAsync(boleto, "TXN-001");

        //a excecao foi silenciada; o anti-pattern funcionou, mas escondeu a falha
        await acao
            .Should()
            .NotThrowAsync(because: "ServicoCobrancaViolacao absorve NotSupportedException silenciosamente " +
                     "— isso documenta o anti-pattern causado pela violacao de LSP em ProcessadorBoleto");
    }
}
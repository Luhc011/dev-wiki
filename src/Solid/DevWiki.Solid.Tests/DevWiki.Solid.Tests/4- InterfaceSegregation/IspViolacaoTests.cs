using DevWiki.Solid.Isp.Violacao;
using DevWiki.Solid.Isp.Violacao.Interfaces;
using DevWiki.Solid.Shared.Domain;
using FluentAssertions;
using Moq;

namespace DevWiki.Solid.Tests._4__InterfaceSegregation;

public class IspViolacaoTests
{
    //ContaCorrente (violacao)

    [Fact(DisplayName = "ContaCorrente (violação): AplicarRendimentoAsync deve lançar NotSupportedException")]
    public async Task ContaCorrente_AplicarRendimento_DeveLancarNotSupportedException()
    {
        /*
         * VIOLACAO DE ISP:
         * ContaCorrente é forcada a implementar AplicarRendimentoAsync pela interface gorda
         * Como conta corrente n rende automaticamente, a unica saida é NotSupportedException
         * mas o compilador n avisa o caller: qlqr codigo com IContaBancariaViolacao generica
         * pode chamar este metodo e explodir em runtime.
         */

        IContaBancariaViolacao conta = new ContaCorrente();

        var acao = async () => await conta.AplicarRendimentoAsync(0.01m);

        //evidencia da violacao: metodo prometido pela interface n funciona
        await acao
            .Should()
            .ThrowAsync<NotSupportedException>(because: "ContaCorrente n possui rendimento automatico mas é obrigada a implementar o metodo");
    }

    [Fact(DisplayName = "ContaCorrente (violacao): ResgatarAsync deve lancar NotSupportedException")]
    public async Task ContaCorrente_Resgatar_DeveLancarNotSupportedException()
    {
        IContaBancariaViolacao conta = new ContaCorrente(500m);

        var acao = async () => await conta.ResgatarAsync(100m, DateOnly.FromDateTime(DateTime.Today.AddDays(30)));

        await acao.Should().ThrowAsync<NotSupportedException>(
            because: "ContaCorrente n possui resgate programado mas é obrigada pela interface gorda");
    }

    //ContaInvestimento (violacao)

    [Fact(DisplayName = "ContaInvestimento (violacao): SacarAsync deve lancar NotSupportedException")]
    public async Task ContaInvestimento_Sacar_DeveLancarNotSupportedException()
    {
        IContaBancariaViolacao conta = new ContaCorrente(500m);

        var acao = async () => await conta.SacarAsync(100m);

        await acao.Should().ThrowAsync<NotSupportedException>(because: "ContaInvestimento n permite saque direto, tem carencia");
    }

    [Fact(DisplayName = "ContaInvestimento (violação): TransferirAsync deve lancar NotSupportedException")]
    public async Task ContaInvestimento_Transferir_DeveLancarNotSupportedException()
    {
        IContaBancariaViolacao conta = new ContaInvestimento(1000m);

        var acao = async () => await conta.TransferirAsync(100m, "12345-6");

        await acao
            .Should()
            .ThrowAsync<NotSupportedException>(because: "ContaInvestimento n suporta transferencia direta pela interface gorda");
    }

    [Fact(DisplayName = "ContaInvestimento (violacao): EmitirCartaoAsync deve lancar NotSupportedException")]
    public async Task ContaInvestimento_EmitirCartao_DeveLancarNotSupportedException()
    {
        IContaBancariaViolacao conta = new ContaInvestimento();

        var acao = async () => await conta.EmitirCartaoAsync();

        await acao
            .Should()
            .ThrowAsync<NotSupportedException>(because: "ContaInvestimento n emite cartao, mas a interface gorda obriga a implementacao");
    }

    [Fact(DisplayName = "ContaInvestimento (violacao): BloquearCartaoAsync deve lancar NotSupportedException")]
    public async Task ContaInvestimento_BloquearCartao_DeveLancarNotSupportedException()
    {
        IContaBancariaViolacao conta = new ContaInvestimento();

        var acao = async () => await conta.BloquearCartaoAsync("4111-1234-5678-9012");

        await acao.Should().ThrowAsync<NotSupportedException>(
            because: "ContaInvestimento n possui cartao, mas deve implementar BloquearCartaoAsync pela interface gorda");
    }

    // ── ContaDigital (violacao) 
    [Fact(DisplayName = "ContaDigital (violacao): AplicarRendimentoAsync deve lancar NotSupportedException")]
    public async Task ContaDigital_AplicarRendimento_DeveLancarNotSupportedException()
    {
        IContaBancariaViolacao conta = new ContaDigital(300m);

        var acao = async () => await conta.AplicarRendimentoAsync(0.005m);

        await acao.Should().ThrowAsync<NotSupportedException>(
            because: "ContaDigital n tem rendimento automatico, mas é forçada pela interface gorda");
    }

    [Fact(DisplayName = "ContaDigital (violacao): EmitirCartaoAsync deve lancar NotSupportedException")]
    public async Task ContaDigital_EmitirCartao_DeveLancarNotSupportedException()
    {
        IContaBancariaViolacao conta = new ContaDigital();

        var acao = async () => await conta.EmitirCartaoAsync();

        await acao.Should().ThrowAsync<NotSupportedException>(
            because: "ContaDigital é 100% app, sem cartao fisico mas deve implementar EmitirCartaoAsync");
    }

    //Interface gorda teste de design 

    [Fact(DisplayName = "Interface gorda: IContaBancariaViolacao.Violation tem 8 metodo, muitos para qlqr implementador unico")]
    public void InterfaceGorda_IContaBancaria_TemOitoMetodosQueNenhumImplementadorUsaTodos()
    {
        /*
         * EVIDENCIA DE ISP VIOLADO:
         * A interface tem 8 metodos, mas nenhum implementador concreto usa todos:
         * - ContaCorrente: 6 reais + 2 NotSupportedException
         * - ContaInvestimento: 4 reais + 4 NotSupportedException
         * - ContaDigital: 4 reais + 4 NotSupportedException
         *
         * Indicador: a media de NotSupportedException por classe é > 0
         * Se qualquer implementador joga NotSupportedException, a interface é gorda
         */

        var tipoInterface = typeof(IContaBancariaViolacao);

        var totalMetodos = tipoInterface.GetMethods().Length;

        totalMetodos.Should().Be(8,
            because: "interface gorda com 8 metodos forca todos os implementadores a lidar com " +
                     "operacaes que n fazem sentido para eles, evidencia direta de ISP violado");
    }

    //ServicoConsultaCobranca — dependencia desnecessaria
    [Fact(DisplayName = "ServicoConsultaCobranca (violaçao): usa 2 de 6 metodos mas depende da interface inteira")]
    public async Task ServicoConsultaCobranca_DependeDe6Metodos_UsaApenas2()
    {
        /*
         * VIOLACAO DE ISP — dependencia desnecessaria via interface gorda:
         *
         * ServicoConsultaCobranca usa APENAS BuscarPorIdAsync e ListarPorStatusAsync.
         * Mas como depende de IRepositorioCobranca (6 metodos), qualquer mudanca em:
         *   - SalvarAsync            → força recompilacao deste servico
         *   - GerarRelatorioAsync    → força recompilacao deste servico
         *   - ExportarCsvAsync       → força recompilacao deste servico
         *   - EnviarNotificacoesPendentesAsync → forca recompilacao deste servico
         *
         * Visivel no teste: o mock precisa ser do tipo IRepositorioCobranca (6 metodos)
         * mesmo que o servico use apenas 2. No design correto, o mock seria de
         * IRepositorioCobrancaLeitura (2 metodos) — sem sobrecarga desnecessaria
         */

        var repositorioMock = new Mock<IRepositorioCobranca>();
        var id = "COB-001";
        var cobranca = new Cobranca(id, 150m, "Mensalidade", new DateOnly(2026, 7, 10));

        repositorioMock
            .Setup(r => r.BuscarPorIdAsync(id))
            .ReturnsAsync(cobranca);

        var servico = new ServicoConsultaCobranca(repositorioMock.Object);

        //apenas BuscarAsync é chamado
        var resultado = await servico.BuscarAsync(id);

        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(id);

        // Os 4 metodos abaixo NUNCA foram chamados, mas o servico está ACOPLADO a eles via interface gorda
        // Qualquer mudanca em SalvarAsync/GerarRelatorioAsync/ExportarCsvAsync/EnviarNotificacoesPendentesAsync
        // foraa recompilacao de ServicoConsultaCobranca, evidencia de dependencia desnecessaria (ISP violado)
        repositorioMock.Verify(r => r.SalvarAsync(It.IsAny<Cobranca>()), Times.Never);
        repositorioMock.Verify(r => r.GerarRelatorioAsync(It.IsAny<DateOnly>()), Times.Never);
        repositorioMock.Verify(r => r.ExportarCsvAsync(), Times.Never);
        repositorioMock.Verify(r => r.EnviarNotificacoesPendentesAsync(), Times.Never);
    }
}
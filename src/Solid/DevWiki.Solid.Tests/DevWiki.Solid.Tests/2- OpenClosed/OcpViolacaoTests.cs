using DevWiki.Solid.Ocp.Violacao;
using DevWiki.Solid.Shared.Domain;
using FluentAssertions;

namespace DevWiki.Solid.Tests._2__OpenClosed;

public class OcpViolacaoTests
{
    [Fact(DisplayName = "CalculadoraTarifa: adicionar novo tipo exige modificar a classe existente")]
    public void CalculadoraTarifa_Calcular_QuandoAdicionarNovoTipo_ExigeModificarClasseExistente()
    {
        var calculadora = new CalculadoraTarifa();

        var acao = () => calculadora.Calcular("DOC", 500m);

        acao.Should().Throw<ArgumentException>(
            because: "a classe n suporta DOC sem ser modificada, isso evidencia a violacao do OCP");
    }

    [Fact(DisplayName = "CalculadoraTarifa: PIX deve retornar zero")]
    public void CalculadoraTarifa_Calcular_Pix_DeveRetornarZero()
    {
        var calculadora = new CalculadoraTarifa();

        var tarifa = calculadora.Calcular("PIX", 1000m);

        tarifa.Should().Be(0m);
    }

    [Fact(DisplayName = "CalculadoraTarifa: TED deve aplicar tarifa minima quando percentual é menor")]
    public void CalculadoraTarifa_Calcular_Ted_DeveAplicarTarifaMinima()
    {
        var calculadora = new CalculadoraTarifa();

        //100 * 0.002 = 0.20 é menor que 5.00
        var tarifa = calculadora.Calcular("TED", 100m);

        tarifa.Should().Be(5.00m);
    }

    [Fact(DisplayName = "CalculadoraTarifa: Boleto deve retornar tarifa fixa")]
    public void CalculadoraTarifa_Calcular_Boleto_DeveRetornarTarifaFixa()
    {
        var calculadora = new CalculadoraTarifa();

        var tarifa = calculadora.Calcular("Boleto", 5000m);

        tarifa.Should().Be(2.50m);
    }

    [Fact(DisplayName = "CalculadoraTarifa: tipo desconhecido deve lancar ArgumentException")]
    public void CalculadoraTarifa_Calcular_TipoDesconhecido_DeveLancarArgumentException()
    {
        var calculadora = new CalculadoraTarifa();

        var acao = () => calculadora.Calcular("CHEQUE", 200m);

        acao.Should()
            .Throw<ArgumentException>()
            .WithParameterName("tipoPagamento")
            .WithMessage("*CHEQUE*");
    }

    [Fact(DisplayName = "GeradorRelatorio: adicionar novo formato exige modificar a classe existente")]
    public void GeradorRelatorio_Gerar_QuandoAdicionarNovoFormato_ExigeModificarClasseExistente()
    {
        var gerador = new GeradorRelatorio();
        var relatorio = CriarRelatorioTeste();

        var acao = () => gerador.Gerar(relatorio, "JSON");

        acao.Should().Throw<ArgumentException>(
            because: "o formato JSON n existe sem modificar a classe — isso evidencia a violacao do OCP");
    }

    [Fact(DisplayName = "GeradorRelatorio: formato TXT deve conter dados da cobranca")]
    public void GeradorRelatorio_Gerar_FormatoTxt_DeveConterDadosDaCobranca()
    {
        var gerador = new GeradorRelatorio();
        var relatorio = CriarRelatorioTeste();

        var saida = gerador.Gerar(relatorio, "TXT");

        saida.Should().Contain("COB-001");
        saida.Should().Contain("Mensalidade");
    }

    [Fact(DisplayName = "GeradorRelatorio: formato CSV deve conter header")]
    public void GeradorRelatorio_Gerar_FormatoCsv_DeveConterHeader()
    {
        var gerador = new GeradorRelatorio();
        var relatorio = CriarRelatorioTeste();

        var saida = gerador.Gerar(relatorio, "CSV");

        saida.Should().StartWith("Id,Valor,Descricao,DataVencimento");
    }

    [Fact(DisplayName = "GeradorRelatorio: formato desconhecido deve lancar ArgumentException")]
    public void GeradorRelatorio_Gerar_FormatoDesconhecido_DeveLancarArgumentException()
    {
        var gerador = new GeradorRelatorio();
        var relatorio = CriarRelatorioTeste();

        var acao = () => gerador.Gerar(relatorio, "PDF");
        
        acao.Should().Throw<ArgumentException>().WithMessage("*PDF*");
    }

    private static RelatorioCobranca CriarRelatorioTeste()
        => new(Periodo: new DateOnly(2026, 7, 13), Cobrancas:
        [
            new("COB-001", 150.00m, "mensalidaed", new DateOnly(2026,7,17)),
            new("COB-002", 300.00m, "mensalidaed", new DateOnly(2026,7,21)),
        ], TotalProcessado: 450.00m);
}

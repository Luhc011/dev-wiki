using DevWiki.Solid.Ocp.Correto;
using DevWiki.Solid.Shared.Domain;
using FluentAssertions;

namespace DevWiki.Solid.Tests._2__OpenClosed;

public class ServicoTarifaTests
{
    private static ServicoTarifa CriarServicoPadrao()
        => new([new TarifaPix(), new TarifaTed(), new TarifaBoleto()]);

    [Fact(DisplayName = "Calcular: PIX deve retornar zero")]
    public void Calcular_Pix_DeveRetornarZero()
    {
        var servico = CriarServicoPadrao();

        var tarifa = servico.Calcular("PIX", 5000m);

        tarifa.Should().Be(0m);
    }

    [Fact(DisplayName = "Calcular: TED com valor baixo deve retornar tarifa minima")]
    public void Calcular_Ted_ComValorBaixo_DeveRetornarTarifaMinima()
    {
        var servico = CriarServicoPadrao();

        //100 * 0.002 = 0.20 < 5.00
        var tarifa = servico.Calcular("TED", 100m);

        tarifa.Should().Be(5.00m);
    }

    [Fact(DisplayName = "Calcular: TED com valor alto deve retornar percentual")]
    public void Calcular_Ted_ComValorAlto_DeveRetornarPercentual()
    {
        var servico = CriarServicoPadrao();

        //10000 * 0.002 = 20.00 > 5.00
        var tarifa = servico.Calcular("TED", 10_000m);

        tarifa.Should().Be(20.00m);
    }

    [Fact(DisplayName = "Calcular: Boleto deve retornar tarifa fixa")]
    public void Calcular_Boleto_DeveRetornarTarifaFixa()
    {
        var servico = CriarServicoPadrao();

        var tarifa = servico.Calcular("Boleto", 99_999m);

        tarifa.Should().Be(2.50m);
    }

    [Fact(DisplayName = "Calcular: DOC deve retornar tarifa correta (extensao sem modificacao)")]
    public void Calcular_Doc_DeveRetornarTarifaCorreta()
    {
        //TarifaDoc adicionada ao servico sem modificar nenhuma classe existente
        var servico = new ServicoTarifa([new TarifaPix(), new TarifaTed(), new TarifaBoleto(), new TarifaDoc()]);

        //5000 * 0.003 = 15.00 > 8.00
        var tarifa = servico.Calcular("DOC", 5_000m);

        tarifa.Should().Be(15.00m);
    }

    [Fact(DisplayName = "Calcular: tipo n registrado deve lancar ArgumentException")]
    public void Calcular_TipoNaoRegistrado_DeveLancarArgumentException()
    {
        var servico = CriarServicoPadrao();

        var acao = () => servico.Calcular("CHEQUE", 1000m);

        acao.Should().Throw<ArgumentException>().WithMessage("*CHEQUE*");
    }

    [Fact(DisplayName = "Calcular: adicionar novo tipo n modifica nenhuma classe existente")]
    public void Calcular_QuandoAdicionarNovoTipo_NenhumaClasseExistenteEModificada()
    {
        /*
         * Contraste com a Violacao:
         * Aqui TarifaDoc é uma nova classe independente injetada via colecao
         * ServicoTarifa, TarifaPix, TarifaTed e TarifaBoleto tem 0 linhas modificadas
         * O teste abaixo comprova que o servico funciona com o
         * tipo novo sem nenhuma alteracao no codigo existente
         *
         * Na violacao, adicionar "DOC" obrigaria a abrir CalculadoraTarifa.cs
         */

        // ServicoTarifa recebe TarifaDoc como extensao via injecao
        var servicoComDoc = new ServicoTarifa(
        [
            new TarifaPix(),
            new TarifaTed(),
            new TarifaBoleto(),
            new TarifaDoc(),   // nova classe, zero modificacao nas existentes
        ]);

        var tarifaPix = servicoComDoc.Calcular("PIX", 1000m);
        var tarifaDoc = servicoComDoc.Calcular("DOC", 1000m);

        //as existentes continuam funcionando, e a nova tbm
        tarifaPix.Should().Be(0m, because: "PIX sempre isento — nenhuma mudança");
        tarifaDoc.Should().Be(8.00m, because: "DOC: 1000 * 0.003 = 3.00 < 8.00, então tarifa mínima");
    }
}

public class ServicoRelatorioTests
{
    private static readonly RelatorioCobranca _relatorioTeste = new(
        Periodo: new DateOnly(2026, 6, 1),
        Cobrancas:
        [
            new("COB-001", 150.00m, "Mensalidade", new DateOnly(2026, 6, 10)),
            new("COB-002", 300.00m, "Taxa de servico", new DateOnly(2026, 6, 15)),
        ],
        TotalProcessado: 450.00m);

    private static ServicoRelatorio CriarServicoPadrao() =>
        new(
        [
            new FormatadorTxt(),
            new FormatadorCsv { Separador = ',' },
            new FormatadorJson(),
        ]);

    [Fact(DisplayName = "Gerar: formato TXT deve conter o período do relatorio")]
    public void Gerar_FormatoTxt_DeveConterPeriodo()
    {
        var servico = CriarServicoPadrao();

        var saida = servico.Gerar(_relatorioTeste, "TXT");

        saida.Should().Contain("2026");
        saida.Should().Contain("COB-001");
    }

    [Fact(DisplayName = "Gerar: formato CSV deve conter header correto")]
    public void Gerar_FormatoCsv_DeveConterHeader()
    {
        var servico = CriarServicoPadrao();

        var saida = servico.Gerar(_relatorioTeste, "CSV");

        saida.Should().StartWith("Id,Valor,Descricao,DataVencimento");
        saida.Should().Contain("COB-001");
    }

    [Fact(DisplayName = "Gerar: formato JSON deve conter chaves da estrutura (extensao sem modificacao)")]
    public void Gerar_FormatoJson_DeveConterChaves()
    {
        //FormatadorJson adicionado sem tocar FormatadorTxt nem FormatadorCsv
        var servico = CriarServicoPadrao();

        var saida = servico.Gerar(_relatorioTeste, "JSON");

        saida.Should().Contain("\"periodo\"");
        saida.Should().Contain("\"totalProcessado\"");
        saida.Should().Contain("\"cobrancas\"");
        saida.Should().Contain("COB-001");
    }

    [Fact(DisplayName = "Gerar: formato n registrado deve lancar ArgumentException")]
    public void Gerar_FormatoNaoRegistrado_DeveLancarArgumentException()
    {
        var servico = CriarServicoPadrao();

        var acao = () => servico.Gerar(_relatorioTeste, "PDF");

        acao.Should().Throw<ArgumentException>().WithMessage("*PDF*");
    }

    [Fact(DisplayName = "Gerar: adc novo formato n modifica nenhuma classe existente")]
    public void Gerar_QuandoAdicionarNovoFormato_NenhumaClasseExistenteEModificada()
    {
        /*
         * Contraste com VIOlacao:
         * FormatadorJson é uma classe nova independente que implementa IFormatadorRelatorio
         * FormatadorTxt, FormatadorCsv e ServicoRelatorio tem 0 linhas modificadas
         *
         * Na violacao (GeradorRelatorio), adicionar JSON exigiria:
         *   - Abrir GeradorRelatorio.cs
         *   - Add case "JSON" no switch
         *   - Add metodo privado GerarJson
         *   - Retestar TXT e CSV para garantir que n houve regressao
         */

        //ServicoRelatorio recebe FormatadorJson via extensao
        var servicoComJson = new ServicoRelatorio(
        [
            new FormatadorTxt(),
            new FormatadorCsv { Separador = ',' },
            new FormatadorJson(), // nova classe, 0 modificacao nas existentes
        ]);

        var txt = servicoComJson.Gerar(_relatorioTeste, "TXT");
        var csv = servicoComJson.Gerar(_relatorioTeste, "CSV");
        var json = servicoComJson.Gerar(_relatorioTeste, "JSON");

        //formatos existentes continuam intactos JSON funciona sem regressoes
        txt.Should().Contain("RELATÓRIO", because: "TXT n foi modificado");
        csv.Should().Contain("Id,Valor", because: "CSV n foi modificado");
        json.Should().Contain("\"periodo\"", because: "JSON adc sem tocar os anteriores");
    }
}
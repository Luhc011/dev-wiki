using DevWiki.Solid.Lsp.Correto;
using DevWiki.Solid.Lsp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;
using FluentAssertions;

namespace DevWiki.Solid.Tests._3__LiskovSubstitution;

public class LiskovTestProcessarAsync
{
    private static readonly SolicitacaoPagamento _solicitacao = new("SOL-LSP-01", 250m, "Pagamento mensalidade", "341");

    [Theory(DisplayName = "ProcessarAsync: qlqr implementacao deve retornar IdTransacao e Status definidos")]
    [InlineData(typeof(ProcessadorPix))]
    [InlineData(typeof(ProcessadorBoleto))]
    [InlineData(typeof(ProcessadorTed))]
    public async Task ProcessarAsync_QualquerImplementacao_DeveRetornarResultadoComIdTransacaoEStatusDefinidos(Type tipoProcessador)
    {
        /*
         * LISKOV TEST:
         * Substitua cada subtipo no lugar de IProcessadorPagamento.
         * O codigo cliente (este teste) n muda — apenas o tipo concreto varia
         * Se qlqr subtipo falhar aqui, o LSP esta violado naquele subtipo
         *
         * Este teste passaria com todos os 3 processadores corretos
         * e FALHARIA com ProcessadorBoleto (violacao) que retorna IdTransacao = null
         */

        //instancia qlqr implementacao sem conhecer o tipo concreto
        var processador = (IProcessadorPagamento)Activator.CreateInstance(tipoProcessador)!;

        var resultado = await processador.ProcessarAsync(_solicitacao);

        //contrato honrado por todos os subtipos
        resultado.IdTransacao
            .Should()
            .NotBeNullOrEmpty(because: $"{tipoProcessador.Name} deve sempre retornar IdTransacao preenchido");

        resultado.Status
            .Should()
            .NotBe(StatusPagamento.Indefinido, because: $"{tipoProcessador.Name} deve retornar status definido");
    }

    [Theory(DisplayName = "ConsultarStatusAsync: qualquer implementacao deve retornar status definido")]
    [InlineData(typeof(ProcessadorPix))]
    [InlineData(typeof(ProcessadorBoleto))]
    [InlineData(typeof(ProcessadorTed))]
    public async Task ConsultarStatusAsync_QualquerImplementacao_DeveRetornarStatusDefinido(Type tipoProcessador)
    {
        var processador = (IProcessadorPagamento)Activator.CreateInstance(tipoProcessador)!;

        var status = await processador.ConsultarStatusAsync("TXN-LISKOV-001");

        status
            .Should()
            .NotBe(StatusPagamento.Indefinido, because: $"{tipoProcessador.Name} deve retornar status definido, contrato base");
    }
}

public class CapacidadesProcessadoresTests
{
    // PIX: implementa cancelamento e comprovante, mas NAO tem janela de horario
    [Fact(DisplayName = "ProcessadorPix: implementa IProcessadorCancelavel")]
    public void ProcessadorPix_ImplementaIProcessadorCancelavel()
        => new ProcessadorPix().Should().BeAssignableTo<IProcessadorCancelavel>();

    [Fact(DisplayName = "ProcessadorPix: implementa IProcessadorComComprovante")]
    public void ProcessadorPix_ImplementaIProcessadorComComprovante()
        => new ProcessadorPix().Should().BeAssignableTo<IProcessadorComComprovante>();

    [Fact(DisplayName = "ProcessadorPix: n implementa IProcessadorComJanela (24/7)")]
    public void ProcessadorPix_NaoImplementaIProcessadorComJanela()
        => new ProcessadorPix().Should()
                               .NotBeAssignableTo<IProcessadorComJanela>(because: "PIX funciona 24/7 — sem restricao de janela de horario");

    // Boleto: apenas o contrato base, sem cancelamento, sem comprovante, sem janela
    [Fact(DisplayName = "ProcessadorBoleto: n implementa IProcessadorCancelavel")]
    public void ProcessadorBoleto_NaoImplementaIProcessadorCancelavel()
        => new ProcessadorBoleto().Should()
                                  .NotBeAssignableTo<IProcessadorCancelavel>(because: "Boleto pago n é cancelavel compilador impede uso em contextos de cancelamento");

    [Fact(DisplayName = "ProcessadorBoleto: n implementa IProcessadorComComprovante")]
    public void ProcessadorBoleto_NaoImplementaIProcessadorComComprovante()
        => new ProcessadorBoleto()
            .Should()
            .NotBeAssignableTo<IProcessadorComComprovante>(because: "Boleto n tem comprovante imediato, so apos compensacao bancaria");

    [Fact(DisplayName = "ProcessadorBoleto: n implementa IProcessadorComJanela")]
    public void ProcessadorBoleto_NaoImplementaIProcessadorComJanela()
        => new ProcessadorBoleto()
                .Should()
                .NotBeAssignableTo<IProcessadorComJanela>(because: "Registro de boleto funciona 24/7 sem restricao de horario");

    // TED: todas as capacidades
    [Fact(DisplayName = "ProcessadorTed: implementa IProcessadorCancelavel")]
    public void ProcessadorTed_ImplementaIProcessadorCancelavel()
        => new ProcessadorTed().Should().BeAssignableTo<IProcessadorCancelavel>();

    [Fact(DisplayName = "ProcessadorTed: implementa IProcessadorComComprovante")]
    public void ProcessadorTed_ImplementaIProcessadorComComprovante()
        => new ProcessadorTed().Should().BeAssignableTo<IProcessadorComComprovante>();

    [Fact(DisplayName = "ProcessadorTed: implementa IProcessadorComJanela")]
    public void ProcessadorTed_ImplementaIProcessadorComJanela()
        => new ProcessadorTed().Should().BeAssignableTo<IProcessadorComJanela>();
}

//ProcessadorTed: testes da janela de horario
public class ProcessadorTedJanelaTests
{
    private readonly ProcessadorTed _sut = new();

    [Fact(DisplayName = "EstaDisponivel: em dia util dentro do horario deve retornar true")]
    public void EstaDisponivel_EmDiaUtilDentroDoHorario_DeveRetornarTrue()
    {
        //segunda-feira 10h (UTC-3)
        var agora = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(-3));

        var disponivel = _sut.EstaDisponivel(agora);

        disponivel.Should().BeTrue(because: "segunda as 10h esta dentro da janela dias uteis 6h-17h");
    }

    [Fact(DisplayName = "EstaDisponivel: em fim de semana deve retornar false")]
    public void EstaDisponivel_EmFimDeSemana_DeveRetornarFalse()
    {
        //sabado 10h
        var agora = new DateTimeOffset(2026, 6, 13, 10, 0, 0, TimeSpan.Zero);

        var disponivel = _sut.EstaDisponivel(agora);

        disponivel.Should().BeFalse(because: "sabado n é dia util, TED indisponivel");
    }

    [Fact(DisplayName = "EstaDisponivel: fora do horario bancario deve retornar false")]
    public void EstaDisponivel_ForaDoHorario_DeveRetornarFalse()
    {
        //segunda-feira 20h
        var agora = new DateTimeOffset(2026, 6, 15, 20, 0, 0, TimeSpan.Zero);

        var disponivel = _sut.EstaDisponivel(agora);

        disponivel.Should().BeFalse(because: "20h esta fora da janela 6h–17h, TED indisponivel");
    }
}

//ServicoCobrancaCorreto: 0 condicionais de tipo, sistema de tipos garante contratos
public class ServicoCobrancaCorretoTests
{
    private static readonly SolicitacaoPagamento _solicitacao = new("SOL-C-001", 500m, "Transferência corporativa", "341");

    [Fact(DisplayName = "ProcessarAsync: com qualquer processador n verifica tipo concreto")]
    public async Task ProcessarAsync_ComQualquerProcessador_NuncaVerificaTipoConcreto()
    {
        /*
         * ServicoCobrancaCorreto.ProcessarAsync NAO contem:
         *   - is ProcessadorPix / is ProcessadorBoleto / is ProcessadorTed
         *   - as ProcessadorX
         *   - typeof(ProcessadorX)
         *   - GetType() comparison
         *
         * A unica verificacao de tipo e:
         *   if (processador is IProcessadorComJanela janela && ...)
         * — que usa ABSTRACAO, n tipo concreto
         *
         * Contraste com ServicoCobrancaViolacao que contem:
         *   if (processador is ProcessadorTed && ...)
         *   if (processador is ProcessadorBoleto)
         */

        var pix = new ProcessadorPix();
        var servico = new ServicoCobrancaCorreto(pix);

        //0 verificacao de tipo concreto internamente
        var resultado = await servico.ProcessarAsync(_solicitacao);

        resultado.IdTransacao.Should().NotBeNullOrEmpty();
        resultado.Status.Should().Be(StatusPagamento.Processado);
    }

    [Fact(DisplayName = "EnviarComprovanteAsync: so aceita IProcessadorComComprovante, compilador garante")]
    public async Task EnviarComprovanteAsync_SoAceitaIProcessadorComComprovante_CompilacaoGarante()
    {
        /*
         * GARANTIA DO COMPILADOR:
         * ProcessadorBoleto n implementa IProcessadorComComprovante
         * O codigo abaixo N COMPILA, erro em tempo de compilacao, n NullReferenceException em runtime:
         *
         *   var boleto = new ProcessadorBoleto();
         *   await servico.EnviarComprovanteAsync(boleto, "ID");  // ← CS1503: cannot convert
         *
         * Na violacao, o caller recebia null e tomava NullReferenceException em runtime
         * Aqui, o sistema de tipos elimina a classe de erros em tempo de compilacao
         */

        var pix = new ProcessadorPix();  // implementa IProcessadorComComprovante
        var servico = new ServicoCobrancaCorreto(pix);
        var idTransacao = Guid.NewGuid().ToString("N");

        //PIX pode ser passado: honra o contrato de comprovante nao-nulo
        var acao = async () => await servico.EnviarComprovanteAsync(pix, idTransacao);

        await acao
            .Should()
            .NotThrowAsync(because: "ProcessadorPix implementa IProcessadorComComprovante e honra contrato de nao-nulidade");
    }

    [Fact(DisplayName = "CancelarAsync: so aceita IProcessadorCancelavel, compilador garante")]
    public async Task CancelarAsync_SoAceitaIProcessadorCancelavel_CompilacaoGarante()
    {
        /*
         * GARANTIA DO COMPILADOR:
         * ProcessadorBoleto n implementa IProcessadorCancelavel
         * Isso NAO COMPILA:
         *
         *   var boleto = new ProcessadorBoleto();
         *   await servico.CancelarAsync(boleto, "ID");  // ← CS1503: cannot convert
         *
         * Na violacao, o caller precisava de try/catch NotSupportedException
         * Aqui, a excecao é impossivel por definicao, sem try/catch necessario
         */

        var pix = new ProcessadorPix();  // implementa IProcessadorCancelavel
        var servico = new ServicoCobrancaCorreto(pix);

        var acao = async () => await servico.CancelarAsync(pix, Guid.NewGuid().ToString("N"));

        //sem NotSupportedException possivel: contrato garantido pelo tipo
        await acao
            .Should()
            .NotThrowAsync(because: "ProcessadorPix implementa IProcessadorCancelavel, NotSupportedException impossivel");
    }
}
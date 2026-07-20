using DevWiki.Solid.Dip.Correto;
using DevWiki.Solid.Dip.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;
using FluentAssertions;
using Moq;

namespace DevWiki.Solid.Tests._5__DependencyInversion;

public class AutorizacaoPagamentoTests
{
    private readonly Mock<IAnalisadorAntifraude> _antifraude = new();
    private readonly Mock<IGatewayPagamento> _gateway = new();
    private readonly Mock<IRepositorioPagamento> _repositorio = new();
    private readonly Mock<INotificadorPagamento> _notificador = new();
    private readonly ServicoAutorizacaoPagamento _sut;

    private static readonly SolicitacaoPagamento _solicitacao =
        new("SOL-DIP-01", 500m, "Pagamento autorização", "341");

    private static readonly ResultadoPagamento _resultadoAprovado =
        new("TXN-DIP-001", StatusPagamento.Processado, "Aprovado");

    public AutorizacaoPagamentoTests() => 
        _sut = new(_antifraude.Object, _gateway.Object, _repositorio.Object, _notificador.Object);

    [Fact(DisplayName = "AutorizarAsync: quando antifraude aprova deve processar o pagamento via gateway")]
    public async Task AutorizarAsync_QuandoAntifraudeAprovado_DeveProcessarPagamento()
    {
        /*
         * DIP correto: este teste não sabe nada sobre Serpro, Oracle ou SMTP.
         * Qualquer implementação das 4 abstrações funcionaria aqui —
         * o serviço de negócio é agnóstico à infraestrutura.
         */

        // Arrange
        _antifraude
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);
        _gateway
            .Setup(g => g.ProcessarAsync(It.IsAny<SolicitacaoPagamento>()))
            .ReturnsAsync(_resultadoAprovado);

        // Act
        var resultado = await _sut.AutorizarAsync(_solicitacao);

        // Assert
        resultado.Status.Should().Be(StatusPagamento.Processado);
        _gateway.Verify(g => g.ProcessarAsync(It.IsAny<SolicitacaoPagamento>()), Times.Once);
    }

    [Fact(DisplayName = "AutorizarAsync: quando antifraude reprova NÃO deve chamar o gateway")]
    public async Task AutorizarAsync_QuandoAntifraudeReprovado_NaoDeveProcessarPagamento()
    {
        // Arrange — antifraude rejeita
        _antifraude
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(false);

        // Act
        var resultado = await _sut.AutorizarAsync(_solicitacao);

        // Assert
        resultado.Status.Should().Be(StatusPagamento.Cancelado);

        // Gateway e repositório NÃO devem ser chamados — short-circuit correto
        _gateway.Verify(g => g.ProcessarAsync(It.IsAny<SolicitacaoPagamento>()), Times.Never);
        _repositorio.Verify(r => r.SalvarAsync(It.IsAny<ResultadoPagamento>()), Times.Never);
    }

    [Fact(DisplayName = "AutorizarAsync: quando aprovado deve salvar resultado no repositório")]
    public async Task AutorizarAsync_QuandoAprovado_DeveSalvarNoRepositorio()
    {
        // Arrange
        _antifraude
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);
        _gateway
            .Setup(g => g.ProcessarAsync(It.IsAny<SolicitacaoPagamento>()))
            .ReturnsAsync(_resultadoAprovado);

        // Act
        await _sut.AutorizarAsync(_solicitacao);

        // Assert — repositório chamado com o resultado retornado pelo gateway
        _repositorio.Verify(
            r => r.SalvarAsync(It.Is<ResultadoPagamento>(res =>
                res.Status == StatusPagamento.Processado)),
            Times.Once);
    }

    [Fact(DisplayName = "AutorizarAsync: quando aprovado deve enviar notificação")]
    public async Task AutorizarAsync_QuandoAprovado_DeveEnviarNotificacao()
    {
        // Arrange
        _antifraude
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);
        _gateway
            .Setup(g => g.ProcessarAsync(It.IsAny<SolicitacaoPagamento>()))
            .ReturnsAsync(_resultadoAprovado);

        // Act
        await _sut.AutorizarAsync(_solicitacao);

        // Assert — notificador chamado com o banco de origem da solicitação
        _notificador.Verify(
            n => n.NotificarAsync(
                _solicitacao.BancoOrigem,
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact(DisplayName = "AutorizarAsync: quando reprovado NÃO deve enviar notificação")]
    public async Task AutorizarAsync_QuandoReprovado_NaoDeveNotificar()
    {
        // Arrange
        _antifraude
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(false);

        // Act
        await _sut.AutorizarAsync(_solicitacao);

        // Assert — notificação não enviada: pagamento cancelado antes do gateway
        _notificador.Verify(
            n => n.NotificarAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact(DisplayName = "AutorizarAsync: antifraude deve receber dados corretos da solicitação")]
    public async Task AutorizarAsync_AntifraudeDeveReceberDadosCorretosDaSolicitacao()
    {
        /*
         * Verifica que o serviço de negócio passa os dados corretos para a abstração.
         * Não importa qual implementação de IAnalisadorAntifraude está sendo usada —
         * o contrato de chamada é sempre o mesmo.
         */

        // Arrange
        _antifraude
            .Setup(a => a.AnalisarRiscoAsync(_solicitacao.IdSolicitacao, _solicitacao.Valor))
            .ReturnsAsync(false); // reprova para simplificar o Act

        // Act
        await _sut.AutorizarAsync(_solicitacao);

        // Assert — antifraude recebeu exatamente os dados da solicitação
        _antifraude.Verify(
            a => a.AnalisarRiscoAsync(_solicitacao.IdSolicitacao, _solicitacao.Valor),
            Times.Once);
    }

    [Fact(DisplayName = "AutorizarAsync: deve retornar IdTransacao gerado pelo gateway")]
    public async Task AutorizarAsync_DeveRetornarIdTransacaoDoGateway()
    {
        /*
         * O serviço de alto nível retorna o que o gateway (baixo nível via abstração) gerou.
         * Sem DIP: o id seria gerado pelo próprio ServicoAutorizacaoPagamento (acoplado).
         * Com DIP: o id vem da abstração — GatewayPixBacen, GatewayTed, qualquer um.
         */

        // Arrange
        var idEsperado = "TXN-GATEWAY-UNICO-12345";
        var resultadoComIdEspecifico = new ResultadoPagamento(
            idEsperado, StatusPagamento.Processado, "OK");

        _antifraude
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);
        _gateway
            .Setup(g => g.ProcessarAsync(It.IsAny<SolicitacaoPagamento>()))
            .ReturnsAsync(resultadoComIdEspecifico);

        // Act
        var resultado = await _sut.AutorizarAsync(_solicitacao);

        // Assert
        resultado.IdTransacao.Should().Be(idEsperado,
            because: "o serviço retorna o IdTransacao gerado pelo gateway — " +
                     "alto nível não conhece nem controla a geração do ID");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Flexibilidade do DIP — substituição de implementação sem modificar o negócio
// 
public class FlexibilidadeDipTests
{
    [Fact(DisplayName = "ServicoAutorizacaoPagamento: pode substituir qualquer dependência sem alterar a classe")]
    public async Task ServicoAutorizacaoPagamento_PodeSubstituirQualquerDependencia_SemAlterarAClasse()
    {
        /*
         * DIP + LSP em conjunto: qualquer implementação de IAnalisadorAntifraude é substituível.
         *
         * Criamos 3 instâncias de ServicoAutorizacaoPagamento com diferentes comportamentos
         * de antifraude — a classe não foi alterada, apenas a implementação injetada.
         *
         * Em produção: trocar Serpro por ClearSale = 1 linha no container de DI.
         */

        // Arrange — Gateway compartilhado entre os 3 serviços
        var gatewayMock = new Mock<IGatewayPagamento>();
        var resultadoAprovado = new ResultadoPagamento("TXN-001", StatusPagamento.Processado, "OK");
        gatewayMock
            .Setup(g => g.ProcessarAsync(It.IsAny<SolicitacaoPagamento>()))
            .ReturnsAsync(resultadoAprovado);

        var repositorio = new Mock<IRepositorioPagamento>();
        var notificador = new Mock<INotificadorPagamento>();

        // Implementação 1: antifraude que SEMPRE aprova
        var antifraudeAprovador = new Mock<IAnalisadorAntifraude>();
        antifraudeAprovador
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);

        // Implementação 2: antifraude que SEMPRE reprova
        var antifraudeReprovador = new Mock<IAnalisadorAntifraude>();
        antifraudeReprovador
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(false);

        // Implementação 3: antifraude condicional — reprova acima de R$ 1.000
        var antifraudeCondicional = new Mock<IAnalisadorAntifraude>();
        antifraudeCondicional
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.Is<decimal>(v => v <= 1_000m)))
            .ReturnsAsync(true);
        antifraudeCondicional
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.Is<decimal>(v => v > 1_000m)))
            .ReturnsAsync(false);

        var solicitacaoBaixa = new SolicitacaoPagamento("SOL-B", 500m, "Valor baixo", "341");
        var solicitacaoAlta = new SolicitacaoPagamento("SOL-A", 5_000m, "Valor alto", "341");

        // ServicoAutorizacaoPagamento não muda — apenas a implementação injetada varia
        var servicoAprovador = new ServicoAutorizacaoPagamento(antifraudeAprovador.Object, gatewayMock.Object, repositorio.Object, notificador.Object);
        var servicoReprovador = new ServicoAutorizacaoPagamento(antifraudeReprovador.Object, gatewayMock.Object, repositorio.Object, notificador.Object);
        var servicoCondicional = new ServicoAutorizacaoPagamento(antifraudeCondicional.Object, gatewayMock.Object, repositorio.Object, notificador.Object);

        // Act
        var resultadoSempreAprova = await servicoAprovador.AutorizarAsync(solicitacaoAlta);
        var resultadoSempreReprova = await servicoReprovador.AutorizarAsync(solicitacaoBaixa);
        var resultadoBaixoCondicional = await servicoCondicional.AutorizarAsync(solicitacaoBaixa);
        var resultadoAltoCondicional = await servicoCondicional.AutorizarAsync(solicitacaoAlta);

        // Assert — mesma classe, comportamentos diferentes via injeção
        resultadoSempreAprova.Status.Should().Be(StatusPagamento.Processado,
            because: "antifraudeAprovador sempre aprova — mesmo R$5.000");
        resultadoSempreReprova.Status.Should().Be(StatusPagamento.Cancelado,
            because: "antifraudeReprovador sempre rejeita — mesmo R$500");
        resultadoBaixoCondicional.Status.Should().Be(StatusPagamento.Processado,
            because: "R$500 ≤ R$1.000 — antifraudeCondicional aprova");
        resultadoAltoCondicional.Status.Should().Be(StatusPagamento.Cancelado,
            because: "R$5.000 > R$1.000 — antifraudeCondicional rejeita");
    }

    [Fact(DisplayName = "ConfiguracaoDi: deve criar ServicoAutorizacaoPagamento com todas as dependências resolvidas")]
    public void ConfiguracaoDi_DeveCriarServicoAutorizacaoComTodasDependencias()
    {
        /*
         * O container de DI conecta abstrações com implementações em runtime.
         * ServicoAutorizacaoPagamento não sabe qual implementação recebeu —
         * nem precisa saber. DIP na prática.
         */

        // Arrange + Act
        var provider = ConfiguracaoDi.CriarContainer();
        var sut = provider.GetService(typeof(ServicoAutorizacaoPagamento))
            as ServicoAutorizacaoPagamento;

        // Assert
        sut.Should().NotBeNull(
            because: "container de DI deve resolver ServicoAutorizacaoPagamento " +
                     "com todas as 4 abstrações registradas");
        sut.Should().BeOfType<ServicoAutorizacaoPagamento>();
    }

    [Fact(DisplayName = "ConfiguracaoDi: deve resolver cada abstração para a implementação correta")]
    public void ConfiguracaoDi_DeveResolverCadaAbstracaoParaImplementacaoCorreta()
    {
        // Arrange
        var provider = ConfiguracaoDi.CriarContainer();

        // Act + Assert — cada abstração resolve para uma implementação concreta não-nula
        var antifraude = provider.GetService(typeof(IAnalisadorAntifraude));
        var gateway = provider.GetService(typeof(IGatewayPagamento));
        var repositorio = provider.GetService(typeof(IRepositorioPagamento));
        var notificador = provider.GetService(typeof(INotificadorPagamento));

        antifraude.Should().NotBeNull(because: "IAnalisadorAntifraude deve estar registrado");
        gateway.Should().NotBeNull(because: "IGatewayPagamento deve estar registrado");
        repositorio.Should().NotBeNull(because: "IRepositorioPagamento deve estar registrado");
        notificador.Should().NotBeNull(because: "INotificadorPagamento deve estar registrado");
    }

    [Fact(DisplayName = "DIP: contraste entre violação e correto em termos de testabilidade")]
    public async Task DiferencaEntreViolacaoECorreta_EmTernosDeTestabilidade()
    {
        /*
         * VIOLAÇÃO — impossível testar com a classe assim (código comentado pois exige infra real):
         *
         *   var violacao = new Violation.ServicoAutorizacaoPagamento();
         *   await violacao.AutorizarAsync(solicitacao);
         *   // falha: sem endpoint Serpro ativo, sem Oracle, sem SMTP
         *
         * CORRETO — 4 mocks, zero infraestrutura, < 10ms:
         */

        // Arrange — nenhum serviço externo necessário
        var antifraude = new Mock<IAnalisadorAntifraude>();
        var gateway = new Mock<IGatewayPagamento>();
        var repositorio = new Mock<IRepositorioPagamento>();
        var notificador = new Mock<INotificadorPagamento>();

        var solicitacao = new SolicitacaoPagamento("SOL-CONTRASTE", 750m, "Teste de contraste", "001");
        var resultadoEsperado = new ResultadoPagamento(
            "TXN-CONTRASTE-001", StatusPagamento.Processado, "OK");

        antifraude
            .Setup(a => a.AnalisarRiscoAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);
        gateway
            .Setup(g => g.ProcessarAsync(It.IsAny<SolicitacaoPagamento>()))
            .ReturnsAsync(resultadoEsperado);

        var servico = new ServicoAutorizacaoPagamento(
            antifraude.Object, gateway.Object, repositorio.Object, notificador.Object);

        // Act — roda sem rede, sem banco, sem SMTP
        var resultado = await servico.AutorizarAsync(solicitacao);

        // Assert
        resultado.Status.Should().Be(StatusPagamento.Processado,
            because: "DIP correto: ServicoAutorizacaoPagamento testável com mocks em < 10ms — " +
                     "sem Serpro, sem Oracle, sem SMTP");
        resultado.IdTransacao.Should().Be("TXN-CONTRASTE-001");
    }
}
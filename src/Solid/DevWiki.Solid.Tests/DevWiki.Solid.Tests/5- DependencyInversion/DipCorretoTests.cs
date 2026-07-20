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

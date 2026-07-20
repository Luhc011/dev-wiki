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
}

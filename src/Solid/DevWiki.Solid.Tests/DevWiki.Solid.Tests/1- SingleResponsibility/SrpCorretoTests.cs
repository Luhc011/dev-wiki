using DevWiki.Solid.Shared.Domain;
using DevWiki.Solid.Srp.Correto;
using FluentAssertions;
using Moq;

namespace DevWiki.Solid.Tests._1__SingleResponsibility;

public class ValidadorSolicitacaoTests
{
    private readonly ValidadorSolicitacao _sut = new();

    [Fact(DisplayName = "Validar: quando valor zero, deve retornar invalido")]
    public void Validar_QuandoValorZero_DeveRetornarInvalido()
    {
        var solicitacao = new SolicitacaoPagamento("SOL-001", 0m, "Mensalidade", "341");

        var resultado = _sut.Validar(solicitacao);

        resultado.IsValido.Should().BeFalse();
        resultado.MensagemErro.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "Validar: quando banco de origem vazio, deve retornar invalido")]
    public void Validar_QuandoBancoOrigemVazio_DeveRetornarInvalido()
    {
        var solicitacao = new SolicitacaoPagamento("SOL-002", 100m, "Mensalidade", "");

        var resultado = _sut.Validar(solicitacao);

        resultado.IsValido.Should().BeFalse();
        resultado.MensagemErro.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "Validar: quando dados validos, deve retornar valido")]
    public void Validar_QuandoDadosValidos_DeveRetornarValido()
    {
        var solicitacao = new SolicitacaoPagamento("SOL-003", 250m, "Mensalidade", "341");

        var resultado = _sut.Validar(solicitacao);

        resultado.IsValido.Should().BeTrue();
        resultado.MensagemErro.Should().BeNull();
    }
}

public class RepositorioPagamentoTests
{
    private readonly RepositorioPagamento _sut = new();

    [Fact(DisplayName = "SalvarAsync: deve permitir buscar o registro posterior")]
    public async Task SalvarAsync_DevePermitirBuscarPosterior()
    {
        // Arrange
        var resultado = new ResultadoPagamento("TXN-ABC123", StatusPagamento.Processado, null);

        // Act
        await _sut.SalvarAsync(resultado);
        var encontrado = await _sut.BuscarAsync("TXN-ABC123");

        // Assert
        encontrado.Should().NotBeNull();
        encontrado!.IdTransacao.Should().Be("TXN-ABC123");
        encontrado.Status.Should().Be(StatusPagamento.Processado);
    }

    [Fact(DisplayName = "BuscarAsync: quando n existe, deve retornar null")]
    public async Task BuscarAsync_QuandoNaoExiste_DeveRetornarNull()
    {
        var encontrado = await _sut.BuscarAsync("TXN-INEXISTENTE");

        encontrado.Should().BeNull();
    }
}

public class NotificacaoServiceTests
{
    [Fact(DisplayName = "NotificarAsync: deve enviar sem lancar excecao")]
    public async Task NotificarAsync_DeveEnviarSemLancarExcecao()
    {
        var sut = new NotificacaoService();

        var acao = async () => await sut.NotificarAsync(
            "cliente@banco.com.br",
            "sua cobranca foi processada.");

        await acao.Should().NotThrowAsync();
    }
}

public class RelatorioServiceTests
{
    private readonly RelatorioService _sut = new();

    [Fact(DisplayName = "Gerar: deve conter o total correto de processados")]
    public void Gerar_DeveConterTotalCorreto()
    {
        var resultados = new[]
        {
            new ResultadoPagamento("TXN-001", StatusPagamento.Processado, null),
            new ResultadoPagamento("TXN-002", StatusPagamento.Cancelado, "Saldo insuficiente"),
            new ResultadoPagamento("TXN-003", StatusPagamento.Processado, null),
        };

        var relatorio = _sut.Gerar(resultados);

        relatorio.Should().Contain("Processados        : 2");
        relatorio.Should().Contain("Total de registros : 3");
    }

    [Fact(DisplayName = "Gerar: deve listar todas as cobrancas no relatorio")]
    public void Gerar_DeveListarTodasAsCobrancas()
    {
        var resultados = new[]
        {
            new ResultadoPagamento("TXN-X1", StatusPagamento.Processado, null),
            new ResultadoPagamento("TXN-X2", StatusPagamento.Cancelado, null),
        };

        var relatorio = _sut.Gerar(resultados);

        relatorio.Should().Contain("TXN-X1");
        relatorio.Should().Contain("TXN-X2");
    }
}

public class ProcessadorCobrancaTests
{
    private readonly Mock<IValidadorSolicitacao> _validadorMock = new();
    private readonly Mock<IRepositorioPagamento> _repositorioMock = new();
    private readonly Mock<INotificacaoService> _notificacaoMock = new();
    private readonly Mock<IRelatorioService> _relatorioMock = new();
    private readonly ProcessadorCobranca _sut;

    private readonly SolicitacaoPagamento _solicitacaoValida = new("SOL-100", 500m, "Parcela de financiamento", "033");

    public ProcessadorCobrancaTests()
    {
        _sut = new ProcessadorCobranca(_validadorMock.Object,
                                       _repositorioMock.Object,
                                       _notificacaoMock.Object,
                                       _relatorioMock.Object);
    }

    [Fact(DisplayName = "ProcessarAsync: quando valido, deve chamar repositorio exatamente uma vez")]
    public async Task ProcessarAsync_QuandoValido_DeveChamarRepositorioUmaVez()
    {
        _validadorMock
            .Setup(v => v.Validar(_solicitacaoValida))
            .Returns(new ResultadoValidacao(true, null));

        _repositorioMock
            .Setup(r => r.SalvarAsync(It.IsAny<ResultadoPagamento>()))
            .Returns(Task.CompletedTask);

        _notificacaoMock
            .Setup(n => n.NotificarAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.ProcessarAsync(_solicitacaoValida);

        _repositorioMock.Verify(r => r.SalvarAsync(It.IsAny<ResultadoPagamento>()), Times.Once);
    }

    [Fact(DisplayName = "ProcessarAsync: quando valido, deve chamar notificacao exatamente uma vez")]
    public async Task ProcessarAsync_QuandoValido_DeveChamarNotificacaoUmaVez()
    {
        _validadorMock
            .Setup(v => v.Validar(_solicitacaoValida))
            .Returns(new ResultadoValidacao(true, null));

        _repositorioMock
            .Setup(r => r.SalvarAsync(It.IsAny<ResultadoPagamento>()))
            .Returns(Task.CompletedTask);

        _notificacaoMock
            .Setup(n => n.NotificarAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.ProcessarAsync(_solicitacaoValida);

        _notificacaoMock.Verify(n => n.NotificarAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact(DisplayName = "ProcessarAsync: quando invalido, n deve chamar repositorio")]
    public async Task ProcessarAsync_QuandoInvalido_NaoDeveChamarRepositorio()
    {
        _validadorMock
            .Setup(v => v.Validar(It.IsAny<SolicitacaoPagamento>()))
            .Returns(new ResultadoValidacao(false, "Valor invalido"));

        await _sut.ProcessarAsync(_solicitacaoValida);

        _repositorioMock.Verify(r => r.SalvarAsync(It.IsAny<ResultadoPagamento>()), Times.Never);
    }

    [Fact(DisplayName = "ProcessarAsync: quando invalido, n deve chamar notificacao")]
    public async Task ProcessarAsync_QuandoInvalido_NaoDeveChamarNotificacao()
    {
        _validadorMock
            .Setup(v => v.Validar(It.IsAny<SolicitacaoPagamento>()))
            .Returns(new ResultadoValidacao(false, "Banco de origem ausente"));

        await _sut.ProcessarAsync(_solicitacaoValida);

        _notificacaoMock.Verify(n => n.NotificarAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "ProcessarAsync: pode testar processamento sem efeitos colaterais reais")]
    public async Task ProcessarAsync_PodeTestarProcessamentoSemEfeitosColateraisReais()
    {
        /*
         * Contraste com a VIOLACAO: 
         * Aqui, cada dependencia é uma interface mockada. O teste verifica o comportamento
         * do processador de forma isolada sem banco de dados real, sem SMTP, sem delays de infra
         * 
         * Na violacao (ServicoCobranca), isso era impossivel: SalvarNoBancoAsync e EnviarNotificacaoAsync
         * eram privados e sempre disparados juntos.
         */

        //mocks configuram comportamento sem infra
        _validadorMock
            .Setup(v => v.Validar(_solicitacaoValida))
            .Returns(new ResultadoValidacao(true, null));

        _repositorioMock
            .Setup(r => r.SalvarAsync(It.IsAny<ResultadoPagamento>()))
            .Returns(Task.CompletedTask);

        _notificacaoMock
            .Setup(n => n.NotificarAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var resultado = await _sut.ProcessarAsync(_solicitacaoValida);

        //verifica apenas comportamento do processqador, sem nenhum efeito colateral de db ou email
        resultado.Status.Should().Be(StatusPagamento.Processado);
        resultado.IdTransacao.Should().NotBeNullOrEmpty();

        _validadorMock.Verify(v => v.Validar(_solicitacaoValida), Times.Once);
        _repositorioMock.Verify(r => r.SalvarAsync(It.Is<ResultadoPagamento>(r => r.Status == StatusPagamento.Processado)), Times.Once);
        _notificacaoMock.Verify(n => n.NotificarAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
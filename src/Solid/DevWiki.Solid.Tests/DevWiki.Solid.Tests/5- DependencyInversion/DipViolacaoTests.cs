using DevWiki.Solid.Dip.Violacao;
using FluentAssertions;
using System.Reflection;

namespace DevWiki.Solid.Tests._5__DependencyInversion;

public class DipViolacaoTests
{
    // ── Estrutura da classe — reflection revela o acoplamento ────────────────

    [Fact(DisplayName = "ServicoAutorizacaoPagamento (violacao): construtor sem parametros impede injecao")]
    public void ServicoAutorizacaoPagamento_NaoEPossivelInjetarDependencias()
    {
        /*
         * VIOLAÇÃO DE DIP — Regra 1:
         * ServicoAutorizacaoPagamento (alto nivel) não expõe nenhum construtor com parametros
         * É impossível injetar SistemaSerpro, OracleRepositorio ou SmtpNotificador alternativos
         * É impossível passar mocks — logo, impossível testar em isolamento
         *
         * CONTRASTE — ServicoAutorizacaoPagamento (Correct):
         *   Primary constructor com 4 parametros de abstracao — totalmente injetavel e mockável.
         */

        // Arrange
        var tipo = typeof(ServicoAutorizacaoPagamento);

        // Act
        var construtores = tipo.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Assert — todos os construtores públicos são sem parâmetros (= acoplamento selado)
        construtores.Should().AllSatisfy(c =>
            c.GetParameters().Should().BeEmpty(
                because: "ServicoAutorizacaoPagamento (violação) não expõe construtor com parâmetros — " +
                         "impossível injetar dependências alternativas ou mocks"));
    }

    [Fact(DisplayName = "ServicoAutorizacaoPagamento (violação): campos privados são tipos concretos, não interfaces")]
    public void ServicoAutorizacaoPagamento_DependeDeConcretizacoes_NaoDeAbstracoes()
    {
        /*
         * VIOLAÇÃO DE DIP — Regra 1 evidenciada por reflection:
         * Os campos da classe são dos tipos concretos SistemaSerpro, OracleRepositorio e SmtpNotificador.
         * Nenhum campo é do tipo de uma abstração (interface ou classe abstrata).
         *
         * No design correto (Correct.ServicoAutorizacaoPagamento), todos os campos
         * são abstrações: IAnalisadorAntifraude, IGatewayPagamento, IRepositorioPagamento, INotificadorPagamento.
         */

        // Arrange
        var tipo = typeof(ServicoAutorizacaoPagamento);

        // Act — filtra campos gerados pelo compilador (backing fields contêm '<')
        var camposDeInstancia = tipo
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => !f.Name.Contains('<'))
            .ToList();

        // Assert — todos os campos são tipos concretos (classes), não interfaces
        camposDeInstancia.Should().NotBeEmpty(
            because: "ServicoAutorizacaoPagamento (violação) deve ter campos de instância declarados");

        camposDeInstancia.Should().AllSatisfy(campo =>
            campo.FieldType.IsInterface.Should().BeFalse(
                because: $"campo '{campo.Name}' é do tipo concreto '{campo.FieldType.Name}' — " +
                         "viola Regra 1 do DIP: alto nível depende diretamente de baixo nível"));
    }

    [Fact(DisplayName = "ServicoAutorizacaoPagamento (violação): testar exigiria Serpro + Oracle + SMTP reais")]
    public void ServicoAutorizacaoPagamento_ParaTestar_PrecisariaDeSistemaSerpro_Oracle_Smtp()
    {
        /*
         * 💥 PROBLEMA DE DESIGN — documentado por este teste:
         *
         * Para testar ServicoAutorizacaoPagamento (violação) precisaríamos de:
         *   1. Endpoint ativo do Serpro (antifraude externo — latência + custo por chamada)
         *   2. Oracle com schema e credenciais de produção (ou ambiente espelho)
         *   3. Servidor SMTP configurado (conta de e-mail real)
         *
         * Consequências:
         *   - Testes lentos: mínimo 35ms por chamada de rede (20ms Serpro + 10ms Oracle + 5ms SMTP)
         *   - Testes frágeis: qualquer serviço indisponível = suite inteira falha
         *   - Não rodam em CI/CD sem infraestrutura especial (VPN, secrets, banco de testes)
         *   - Impossível simular: antifraude reprovando, Oracle offline, SMTP falhando
         *   - Custo financeiro: chamadas ao Serpro têm custo por requisição
         *
         * CONTRASTE — ServicoAutorizacaoPagamento (Correct):
         *   Arrange: 4 mocks em < 1ms
         *   Act: AutorizarAsync sem nenhuma chamada de rede
         *   Assert: qualquer cenário é simulável
         *   Tempo total: < 10ms
         *
         * A instanciação abaixo (comentada) materializa o acoplamento:
         *   var servico = new ServicoAutorizacaoPagamento();   ← OK, compila
         *   await servico.AutorizarAsync(solicitacao);          ← sem Serpro + Oracle + SMTP = falha
         */

        // Este teste passa sempre — documenta o impossível sem executá-lo
        true.Should().BeTrue(
            because: "teste de design: DIP violado torna testes funcionais impossíveis " +
                     "sem toda a infraestrutura real disponível");
    }
}
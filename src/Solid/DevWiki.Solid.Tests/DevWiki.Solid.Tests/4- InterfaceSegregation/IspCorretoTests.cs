using DevWiki.Solid.Isp.Correto;
using DevWiki.Solid.Isp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;
using FluentAssertions;
using Moq;

namespace DevWiki.Solid.Tests._4__InterfaceSegregation;

public class CapacidadesContasTests
{
    [Fact(DisplayName = "ContaCorrente: implementa IContaSacavel")]
    public void ContaCorrente_ImplementaIContaSacavel() =>
        new ContaCorrente().Should().BeAssignableTo<IContaSacavel>();

    [Fact(DisplayName = "ContaCorrente: implementa IContaTransferivel")]
    public void ContaCorrente_ImplementaIContaTransferivel() =>
        new ContaCorrente().Should().BeAssignableTo<IContaTransferivel>();

    [Fact(DisplayName = "ContaCorrente: implementa IContaComCartao")]
    public void ContaCorrente_ImplementaIContaComCartao() =>
        new ContaCorrente().Should().BeAssignableTo<IContaComCartao>();

    [Fact(DisplayName = "ContaCorrente: n implementa IContaRendimento, compilador impede chamada invalida")]
    public void ContaCorrente_NaoImplementaIContaRendimento() =>
        new ContaCorrente().Should().NotBeAssignableTo<IContaRendimento>(
            because: "conta corrente n rende automaticamente — ISP impede que o caller tente chamar AplicarRendimentoAsync");

    // ContaInvestimento: apenas rendimento. Sem saque, transferencia ou cartao
    [Fact(DisplayName = "ContaInvestimento: implementa IContaRendimento")]
    public void ContaInvestimento_ImplementaIContaRendimento() =>
        new ContaInvestimento().Should().BeAssignableTo<IContaRendimento>();

    [Fact(DisplayName = "ContaInvestimento: n implementa IContaSacavel — compilador impede saque direto")]
    public void ContaInvestimento_NaoImplementaIContaSacavel() =>
        new ContaInvestimento().Should().NotBeAssignableTo<IContaSacavel>(
            because: "conta investimento tem carencia — saque direto não faz sentido; ISP impede chamada em compilacao");

    [Fact(DisplayName = "ContaInvestimento: n implementa IContaTransferivel")]
    public void ContaInvestimento_NaoImplementaIContaTransferivel() =>
        new ContaInvestimento().Should().NotBeAssignableTo<IContaTransferivel>(
            because: "conta investimento não transfere diretamente — precisa de resgate primeiro");

    [Fact(DisplayName = "ContaInvestimento: não implementa IContaComCartao")]
    public void ContaInvestimento_NaoImplementaIContaComCartao() =>
        new ContaInvestimento().Should().NotBeAssignableTo<IContaComCartao>(
            because: "conta investimento n possui cartao bancario");

    // ContaDigital: saque + transferencia. Sem cartao fisico nem rendimento.
    [Fact(DisplayName = "ContaDigital: implementa IContaSacavel")]
    public void ContaDigital_ImplementaIContaSacavel() =>
        new ContaDigital().Should().BeAssignableTo<IContaSacavel>();

    [Fact(DisplayName = "ContaDigital: implementa IContaTransferivel")]
    public void ContaDigital_ImplementaIContaTransferivel() =>
        new ContaDigital().Should().BeAssignableTo<IContaTransferivel>();

    [Fact(DisplayName = "ContaDigital: n implementa IContaComCartao — 100% app, sem cartao fisico")]
    public void ContaDigital_NaoImplementaIContaComCartao() =>
        new ContaDigital().Should().NotBeAssignableTo<IContaComCartao>(
            because: "conta digital é 100% app — ISP impede caller de tentar emitir cartao fisico");

    [Fact(DisplayName = "ContaDigital: n implementa IContaRendimento")]
    public void ContaDigital_NaoImplementaIContaRendimento() =>
        new ContaDigital().Should().NotBeAssignableTo<IContaRendimento>(
            because: "conta digital simplificada n possui rendimento automatico");
}


// Comportamentos das contas corretas — sem NotSupportedException

public class ComportamentoContasCorretasTests
{
    [Fact(DisplayName = "ContaCorrente: SacarAsync deve reduzir saldo")]
    public async Task ContaCorrente_SacarAsync_DeveReduzirSaldo()
    {
        IContaSacavel conta = new ContaCorrente(saldoInicial: 1000m);


        await conta.SacarAsync(300m);
        var saldo = await conta.ConsultarSaldoAsync();

        saldo.Should().Be(700m, because: "saque de R$300 de saldo inicial R$1000 deve resultar em R$700");
    }


    [Fact(DisplayName = "ContaInvestimento: AplicarRendimentoAsync deve aumentar saldo")]
    public async Task ContaInvestimento_AplicarRendimentoAsync_DeveAumentarSaldo()
    {
        /*
         * ISP correto: ContaInvestimento implementa IContaRendimento
         * AplicarRendimentoAsync funciona de verdade — zero NotSupportedException
         *
         * Contraste com a violacao onde ContaCorrente.AplicarRendimentoAsync lanca NotSupportedException.
         */

        IContaRendimento conta = new ContaInvestimento(saldoInicial: 10_000m);

        await conta.AplicarRendimentoAsync(0.01m); // 1% de rendimento
        var saldo = await conta.ConsultarSaldoAsync();

        saldo.Should().Be(10_100m, because: "rendimento de 1% sobre R$10.000 deve resultar em R$10.100");
    }

    [Fact(DisplayName = "ContaInvestimento: ResgatarAsync com data futura deve executar sem excecao")]
    public async Task ContaInvestimento_ResgatarAsync_DataFutura_DeveExecutar()
    {
        IContaRendimento conta = new ContaInvestimento(5_000m);
        var dataResgate = DateOnly.FromDateTime(DateTime.Today.AddDays(90)); // 90 dias futuro

        var acao = async () => await conta.ResgatarAsync(2_000m, dataResgate);

        //data futura valida: nenhuma excecao
        await acao.Should().NotThrowAsync(
            because: "data de resgate 90 dias no futuro é valida, contrato honrado");
    }

    [Fact(DisplayName = "ContaDigital: TransferirAsync deve executar sem excecao")]
    public async Task ContaDigital_TransferirAsync_DeveExecutarSemErro()
    {
        /*
         * ISP correto: ContaDigital implementa IContaTransferivel.
         * TransferirAsync é real e funciona — zero NotSupportedException.
         * Contraste com ContaInvestimento (violação) onde TransferirAsync lanca excecao.
         */

        IContaTransferivel conta = new ContaDigital(saldoInicial: 500m);

        var acao = async () => await conta.TransferirAsync(200m, "98765-4");

        await acao.Should().NotThrowAsync(because: "ContaDigital implementa IContaTransferivel com comportamento real, sem NotSupportedException");
    }

    [Fact(DisplayName = "ServicoConsultaCobrancaCorreto: depende apenas de IRepositorioCobrancaLeitura (2 metodos)")]
    public async Task ServicoConsultaCobrancaCorreto_DependeApenasDeIRepositorioCobrancaLeitura()
    {
        /*
        * CONTRASTE COM A VIOLACAO:
        *
        * Violação: ServicoConsultaCobranca(IRepositorioCobranca) — 6 metodos no mock.
        * Correto: ServicoConsultaCobrancaCorreto(IRepositorioCobrancaLeitura) — 2 métodos.
        *
        * Este mock tem APENAS 2 mtodos para configurar.
        * Mudanças em IRepositorioCobrancaEscrita, IRelatorioCobrancaService,
        * IExportadorCobranca ou INotificadorCobranca não afetam este servico.
        */

        //mock com APENAS 2 métodos (interface segregada)
        var repositorioMock = new Mock<IRepositorioCobrancaLeitura>();
        var id = "COB-CORRECT-001";
        var cobranca = new Cobranca(id, 200m, "anuidade", new DateOnly(2026, 8, 1));

        repositorioMock
            .Setup(r => r.BuscarPorIdAsync(id))
            .ReturnsAsync(cobranca);

        var servico = new ServicoConsultaCobrancaCorreto(repositorioMock.Object);

        var resultado = await servico.BuscarAsync(id);

        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(id);
        resultado.Valor.Should().Be(200m);
        repositorioMock.Verify(r => r.BuscarPorIdAsync(id), Times.Once);
    }

    [Fact(DisplayName = "BuscarPorIdAsync: quando cobranCa existe deve retornar cobranCaa correta")]
    public async Task BuscarPorIdAsync_QuandoExiste_DeveRetornarCobranca()
    {
        var repositorio = new RepositorioCobrancaLeitura();
        var cobranca = new Cobranca("COB-002", 350m, "Plano saude", new DateOnly(2026, 9, 15));
        repositorio.Adicionar(cobranca, StatusPagamento.Pendente);

        var servico = new ServicoConsultaCobrancaCorreto(repositorio);

        var resultado = await servico.BuscarAsync("COB-002");

        resultado.Should().NotBeNull();
        resultado!.Valor.Should().Be(350m);
        resultado.Descricao.Should().Be("Plano saude");
    }

    [Fact(DisplayName = "ListarPorStatusAsync: deve retornar apenas cobrancas filtradas pelo status")]
    public async Task ListarPorStatusAsync_DeveRetornarFiltrado()
    {
        var repositorio = new RepositorioCobrancaLeitura();

        repositorio.Adicionar(new Cobranca("COB-P1", 100m, "Mensalidade jan", new DateOnly(2026, 1, 10)), StatusPagamento.Pendente);
        repositorio.Adicionar(new Cobranca("COB-P2", 100m, "Mensalidade fev", new DateOnly(2026, 2, 10)), StatusPagamento.Pendente);
        repositorio.Adicionar(new Cobranca("COB-P3", 100m, "Mensalidade mar", new DateOnly(2026, 3, 10)), StatusPagamento.Processado);

        var servico = new ServicoConsultaCobrancaCorreto(repositorio);

        //lista apenas Pendente
        var pendentes = await servico.ListarPendentesAsync();

        //assert
        pendentes.Should().HaveCount(2,
            because: "apenas 2 cobrancas tem status Pendente — filtro por status funciona corretamente");
        pendentes.Should().AllSatisfy(c => c.Id.Should().StartWith("COB-P"),
            because: "todas as cobrancas pendentes tem prefixo COB-P");
    }

    // ISP + LSP — interfaces segregadas eliminam NotSupportedException = LSP respeitado
    public class IspLspIntegradoresTests
    {
        [Fact(DisplayName = "ContaInvestimento: passada onde IContaRendimento é exigida — substituivel sem excecao")]
        public async Task ContaInvestimento_PassadaOndeIContaRendimentoEExigida_SubstituivelSemExcecao()
        {
            /*
             * ISP correto → LSP automaticamente respeitado:
             *
             * Quando ContaInvestimento é passada como IContaRendimento, ela honra
             * TODOS os metodos da interface — AplicarRendimentoAsync e ResgatarAsync funcionam.
             *
             * Na violação, ContaCorrente passada como IContaBancaria pode lancar
             * NotSupportedException em AplicarRendimentoAsync — LSP violado.
             * Aqui isso é impossível: ContaInvestimento só implementa o que pode fazer.
             */

            // Arrange — tipo estatico é IContaRendimento, n a classe concreta
            IContaRendimento conta = new ContaInvestimento(saldoInicial: 5_000m);

            //usa apenas os metodos que ContaInvestimento pode honrar
            await conta.DepositarAsync(1_000m);
            await conta.AplicarRendimentoAsync(0.005m); // 0,5% sobre R$6.000 = R$30
            var saldo = await conta.ConsultarSaldoAsync();

            // Assert
            saldo.Should().BeGreaterThan(6_000m,
                because: "rendimento de 0,5% foi aplicado sobre R$6.000 — contrato honrado sem NotSupportedException");
        }

        [Fact(DisplayName = "ContaCorrente: passada onde IContaSacavel é exigida — substituivel sem excecao")]
        public async Task ContaCorrente_PassadaOndeIContaSavelEExigida_SubstituivelSemExcecao()
        {
            /*
             * ISP correto: ContaCorrente implementa IContaSacavel e honra o contrato.
             * Nenhum caller que usa IContaSacavel pode acidentalmente chamar AplicarRendimentoAsync:
             * o compilador n permite — a interface n tem esse metodo.
             */

            IContaSacavel conta = new ContaCorrente(saldoInicial: 2_000m);

            await conta.SacarAsync(500m);
            var saldo = await conta.ConsultarSaldoAsync();

            saldo.Should().Be(1_500m,
                because: "ContaCorrente honra IContaSacavel: saque reduz saldo — LSP respeitado");
        }

        [Fact(DisplayName = "ContaDigital: passada onde IContaSacavel é exigida — substituível sem exceção")]
        public async Task ContaDigital_PassadaOndeIContaSavelEExigida_SubstituivelSemExcecao()
        {
            /*
             * ISP garante que ContaDigital e ContaCorrente sao intercambiaveis como IContaSacavel.
             * Qualquer novo tipo que implemente IContaSacavel com comportamento real
             * é automaticamente um substituto valido de Liskov — ISP e LSP em sinergia.
             */

            IContaSacavel conta = new ContaDigital(saldoInicial: 800m);

            await conta.SacarAsync(200m);
            var saldo = await conta.ConsultarSaldoAsync();

            saldo.Should().Be(600m,
                because: "ContaDigital honra IContaSacavel: saque funciona sem NotSupportedException");
        }
    }
}

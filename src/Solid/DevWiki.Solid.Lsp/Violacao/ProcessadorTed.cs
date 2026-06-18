using DevWiki.Solid.Lsp.Violacao.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Violacao;

public class ProcessadorTed : IProcessadorPagamento
{
    private readonly Func<DateTimeOffset> _relogio;
    private TimeOnly InicioJanela { get; init; } = new(6, 0);
    private TimeOnly FimJanela { get; init; } = new(17, 0);

    public ProcessadorTed(Func<DateTimeOffset>? relogio = null)
        => _relogio = relogio ?? (() => DateTimeOffset.Now);

    public Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
    {
        var agora = _relogio();
        var hora = TimeOnly.FromTimeSpan(agora.TimeOfDay);
        var ehDiaUtil = agora.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

        if (!ehDiaUtil || hora < InicioJanela || hora >= FimJanela)
            throw new InvalidOperationException($"TED so processa das {InicioJanela} as {FimJanela} em dias uteis. Hora: {hora}");

        return Task.FromResult(new ResultadoPagamento(Guid.NewGuid().ToString("N"), StatusPagamento.Processado, null));
    }

    public Task<Comprovante> GerarComprovanteAsync(string idTransacao)
    {
        var comprovante = new Comprovante(
            IdTransacao: idTransacao,
            DataHora: DateTimeOffset.Now,
            Valor: 0m,
            Descricao: "TED processada");

        return Task.FromResult(comprovante);
    }

    public Task CancelarAsync(string idTransacao)
        => throw new NotSupportedException("TED liquidado n pode ser cancelado");

    public Task<StatusPagamento> ConsultarStatusAsync(string idTransacao) =>
        Task.FromResult(StatusPagamento.Processado);
}

using DevWiki.Solid.Lsp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Lsp.Correto;

public class ProcessadorTed : IProcessadorCancelavel, IProcessadorComComprovante, IProcessadorComJanela
{
    public string DescricaoJanela
    {
        get;
        set => field = value is not null ? value : throw new ArgumentNullException(nameof(value));
    } = "dias uteis das 6h as 17h";

    public bool EstaDisponivel(DateTimeOffset agora)
    {
        var hora = TimeOnly.FromTimeSpan(agora.TimeOfDay);

        return agora.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
            && hora >= new TimeOnly(6, 0)
            && hora < new TimeOnly(17, 0);
    }

    public Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento solicitacao)
        => Task.FromResult(new ResultadoPagamento(Guid.NewGuid().ToString("N"), StatusPagamento.Processado, null));

    public Task<Comprovante> GerarComprovanteAsync(string idTransacao)
        => Task.FromResult(new Comprovante(idTransacao, DateTimeOffset.Now, 0m, "ted processado com ssucesso"));

    public async Task CancelarAsync(string idTransacao)
    {
        await Task.Delay(15);
        Console.WriteLine($"[ted] transacao {idTransacao} cancelada");
    }

    public Task<StatusPagamento> ConsultarStatusAsync(string idTransacao)
        => Task.FromResult(StatusPagamento.Processado);
}

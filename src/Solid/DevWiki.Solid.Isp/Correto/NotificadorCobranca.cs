using DevWiki.Solid.Isp.Correto.Interfaces;
using DevWiki.Solid.Shared.Domain;

namespace DevWiki.Solid.Isp.Correto;

public class NotificadorCobranca(IRepositorioCobrancaLeitura repositorio) : INotificadorCobranca
{
    public async Task NotificarPendentesAsync()
    {
        var pendentes = await repositorio.ListarPorStatusAsync(StatusPagamento.Pendente);

        foreach (var cobranca in pendentes)
            Console.WriteLine($"[isp notificacao] cobranca pendente: {cobranca.Id} — R$ {cobranca.Valor:F2}");
    }
}

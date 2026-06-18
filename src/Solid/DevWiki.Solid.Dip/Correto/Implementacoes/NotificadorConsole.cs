using DevWiki.Solid.Dip.Correto.Interfaces;

namespace DevWiki.Solid.Dip.Correto.Implementacoes;

public class NotificadorConsole : INotificadorPagamento
{
    public async Task NotificarAsync(string destinatario, string mensagem)
    {
        await Task.Delay(5);

        var separador = """
            ─────────────────────────────────────────
            """;

        Console.WriteLine(separador);
        Console.WriteLine($"[notificacao] para: {destinatario}");
        Console.WriteLine($"mensagem: {mensagem}");
        Console.WriteLine($"data/hora: {DateTimeOffset.Now:dd/MM/yyyy HH:mm:ss zzz}");
        Console.WriteLine(separador);
    }
}
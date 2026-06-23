using DevWiki.CleanArch.Domain.Interfaces;

namespace DevWiki.CleanArch.Infrasctructure.Notifications;

internal sealed class NotificadorConsole : INotificadorCobranca
{
    public async Task NotificarAsync(string destinatario, string mensagem, CancellationToken ct = default)
    {
        await Task.Delay(1, ct);

        var separador = """
            ══════════════════════════════════════════════════
            """;

        Console.WriteLine(separador);
        Console.WriteLine($"[NOTIFICACAO] Para: {destinatario}");
        Console.WriteLine($"[NOTIFICACAO] Mensagem: {mensagem}");
        Console.WriteLine(separador);
    }
}
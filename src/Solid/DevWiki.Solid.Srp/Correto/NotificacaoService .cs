namespace DevWiki.Solid.Srp.Correto;

public class NotificacaoService : INotificacaoService
{
    public async Task NotificarAsync(string destinatario, string mensagem)
    {
        await Task.Delay(5);
        Console.WriteLine("[Notfcacao] Para: {0}\n{1}", destinatario, mensagem);
    }
}
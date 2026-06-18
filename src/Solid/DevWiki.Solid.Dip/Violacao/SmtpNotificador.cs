namespace DevWiki.Solid.Dip.Violacao;

public class SmtpNotificador
{
    private readonly string _host = "smtp.banco.internal";
    private readonly int _porta = 587;

    public async Task NotificarAsync(string destinatario, string mensagem)
    {
        await Task.Delay(5);
        Console.WriteLine($"[smtp {_host}:{_porta}] para: {destinatario} | {mensagem}");
    }
}
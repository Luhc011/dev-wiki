namespace DevWiki.CleanArch.Domain.Interfaces;

public interface INotificadorCobranca
{
    Task NotificarAsync(string destinatario, string mensagem, CancellationToken ct = default);
}
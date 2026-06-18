namespace DevWiki.Solid.Dip.Correto.Interfaces;

public interface INotificadorPagamento
{
    Task NotificarAsync(string destinatario, string mensagem);
}

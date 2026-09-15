using DevWiki.Microservices.Outbox.Interfaces;

namespace DevWiki.Microservices.Outbox.Fakes;

public sealed class PublicadorMensagensFake : IPublicadorMensagens
{
    private readonly List<OutboxEvento> _publicados = [];
    private bool _falharProximaPublicacao;

    public IReadOnlyList<OutboxEvento> Publicados => [.. _publicados];
    public int TotalPublicados => _publicados.Count;

    public void ForcarFalha() => _falharProximaPublicacao = true;

    public Task<bool> PublicarAsync(OutboxEvento evento, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evento, nameof(evento));

        if (_falharProximaPublicacao)
        {
            _falharProximaPublicacao = false;
            return Task.FromResult(false);
        }

        _publicados.Add(evento);
        return Task.FromResult(true);
    }
}
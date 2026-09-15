using DevWiki.Microservices.Outbox.Interfaces;

namespace DevWiki.Microservices.Outbox.Workers;

public sealed class InboxProcessor(IInboxRepositorio inbox)
{
    public async Task ProcessarAsync(Guid mensagemId,
                                     string nomeEvento,
                                     Func<Task> processamento,
                                     CancellationToken ct = default)
    {
        if (await inbox.JaProcessadoAsync(mensagemId, ct)) return;

        await processamento();

        await inbox.RegistrarAsync(new InboxEvento
        {
            MensagemId = mensagemId,
            NomeEvento = nomeEvento
        }, ct);
    }
}
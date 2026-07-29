namespace DevWiki.EventDriven.Messaging.RabbitMQ;

public sealed class FilaRabbitMQ
{
    private readonly Queue<RabbitMQMensagem> _fila = new();
    private readonly Dictionary<Guid, RabbitMQMensagem> _emVoo = [];
    private readonly List<RabbitMQMensagem> _dlq = [];

    public string Nome { get; }

    public int MaxTentativas
    {
        get;
        set => field = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "MaxTentativas deve ser positivo");
    } = 3;

    public int TotalNaFila => _fila.Count;
    public int TotalEmVoo => _emVoo.Count;
    public IReadOnlyList<RabbitMQMensagem> DeadLetterQueue => [.. _dlq];

    public FilaRabbitMQ(string nome) => Nome = nome;

    public void Enfileirar(RabbitMQMensagem mensagem) => _fila.Enqueue(mensagem);

    public RabbitMQMensagem? Desenfileirar()
    {
        if (!_fila.TryDequeue(out var mensagem))
            return null;

        mensagem.IncrementarTentativa();
        _emVoo[mensagem.MensagemId] = mensagem;
        return mensagem;
    }

    public void Ack(Guid mensagemId) => _emVoo.Remove(mensagemId);

    public void Nack(Guid mensagemId, bool requeue = true)
    {
        if (!_emVoo.TryGetValue(mensagemId, out var mensagem))
            return;

        _emVoo.Remove(mensagemId);

        if (requeue && mensagem.TentativasEntrega < MaxTentativas)
            _fila.Enqueue(mensagem);
        else
            _dlq.Add(mensagem);
    }
}

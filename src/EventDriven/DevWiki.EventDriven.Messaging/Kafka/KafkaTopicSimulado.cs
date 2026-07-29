namespace DevWiki.EventDriven.Messaging.Kafka;

public sealed class KafkaTopicSimulado
{
    private readonly Dictionary<int, List<KafkaMensagem>> _partitions;
    private readonly Dictionary<int, long> _offsets = [];
    private int _roundRobinIndex;

    public string Nome { get; }

    public int TotalPartitions
    {
        get;
        private set => field = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "TotalPartitions deve ser positivo");
    }

    public KafkaTopicSimulado(string nome, int totalPartitions = 3)
    {
        Nome = nome;
        TotalPartitions = totalPartitions;
        _partitions = Enumerable.Range(0, totalPartitions)
            .ToDictionary(i => i, _ => new List<KafkaMensagem>());

        foreach (var i in Enumerable.Range(0, totalPartitions))
            _offsets[i] = 0;
    }

    public KafkaMensagem Publicar(string payload, string? key = null)
    {
        var partition = key is not null
            ? Math.Abs(key.GetHashCode()) % TotalPartitions
            : _roundRobinIndex++ % TotalPartitions;

        var offset = _offsets[partition]++;
        var msg = new KafkaMensagem(Nome, key, payload, partition, offset, DateTimeOffset.UtcNow);
        _partitions[partition].Add(msg);

        return msg;
    }

    public IReadOnlyList<KafkaMensagem> LerPartition(int partition) => [.. _partitions[partition]];
    public IReadOnlyList<KafkaMensagem> LerAPartirDe(int partition, long offsetInicial)
        => [.. _partitions[partition].Where(m => m.Offset >= offsetInicial)];
    public int TotalMensagens => _partitions.Values.Sum(p => p.Count);
}

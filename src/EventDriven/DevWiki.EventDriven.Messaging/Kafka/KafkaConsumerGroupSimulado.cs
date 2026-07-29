namespace DevWiki.EventDriven.Messaging.Kafka;

public sealed class KafkaConsumerGroupSimulado(string grupoId, KafkaTopicSimulado topico)
{
    private readonly Dictionary<int, long> _offsets = [];

    public string GroupId { get; } = grupoId;

    public IReadOnlyList<KafkaMensagem> Consumir(int partition)
    {
        _offsets.TryGetValue(partition, out var offsetAtual);
        return topico.LerAPartirDe(partition, offsetAtual);
    }

    public void CommitOffset(int partition, long proximoOffset)
        => _offsets[partition] = proximoOffset;

    public long ObterOffset(int partition)
    {
        _offsets.TryGetValue(partition, out var offset);
        return offset;
    }
}
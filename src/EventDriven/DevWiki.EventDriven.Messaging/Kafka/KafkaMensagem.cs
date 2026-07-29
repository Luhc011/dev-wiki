namespace DevWiki.EventDriven.Messaging.Kafka;

public sealed record KafkaMensagem(string Topic,
                                   string? Key,
                                   string Payload,
                                   int Partition,
                                   long Offset,
                                   DateTimeOffset Timestamp);

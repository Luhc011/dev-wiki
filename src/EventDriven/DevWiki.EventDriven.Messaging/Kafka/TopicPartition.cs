namespace DevWiki.EventDriven.Messaging.Kafka;

public sealed record TopicPartition(string Topic, int Partition);
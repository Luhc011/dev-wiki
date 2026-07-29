namespace DevWiki.EventDriven.EventSourcing.Snapshots;

public sealed record Snapshot(Guid AggregateId, long Versao, string Estado, DateTimeOffset CriadoEm);

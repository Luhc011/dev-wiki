namespace DevWiki.Microservices.Shared.SharedKernel;

public abstract record Evento
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset OcorridoEm { get; } = DateTimeOffset.Now;
    public abstract string NomeEvento { get; }
}
namespace DevWiki.CleanArch.Domain.ValueObjects;

public abstract class Entity(Guid id)
{
    public Guid Id { get; } = id == Guid.Empty ? throw new ArgumentException("id n podew ser vazio", nameof(id)) : id;

    public override bool Equals(object? obj)
        => obj is Entity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right)
        => !(left == right);
}
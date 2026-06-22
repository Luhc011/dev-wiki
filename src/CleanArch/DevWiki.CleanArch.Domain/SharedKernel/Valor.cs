using DevWiki.CleanArch.Domain.ValueObjects;

namespace DevWiki.CleanArch.Domain.SharedKernel;

public sealed class Valor
{
    private Valor(decimal quantia, string moeda)
    {
        Quantia = quantia;
        Moeda = moeda;
    }

    public decimal Quantia { get; }
    public string Moeda { get; }

    public static Result<Valor> Criar(decimal quantia, string moeda = "BRL")
    {
        if (quantia <= 0m)
            return Result<Valor>.Falha("Quantia deve ser positiva.");
        if (string.IsNullOrWhiteSpace(moeda))
            return Result<Valor>.Falha("Moeda n pode ser vazia.");
        return Result<Valor>.Ok(new Valor(quantia, moeda));
    }

    public override string ToString() => $"R$ {Quantia:N2}";

    public override bool Equals(object? obj) =>
        obj is Valor other && Quantia == other.Quantia && Moeda == other.Moeda;

    public override int GetHashCode() => HashCode.Combine(Quantia, Moeda);

    public static bool operator ==(Valor? left, Valor? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Valor? left, Valor? right) => !(left == right);
}
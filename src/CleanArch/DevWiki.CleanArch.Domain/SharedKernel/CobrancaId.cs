namespace DevWiki.CleanArch.Domain.SharedKernel;

public readonly record struct CobrancaId(Guid Valor)
{
    public static CobrancaId Novo() => new(Guid.NewGuid());
    public static CobrancaId De(Guid valor) => new(valor);

    public override string ToString() => Valor.ToString();
}

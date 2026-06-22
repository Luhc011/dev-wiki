using DevWiki.CleanArch.Domain.ValueObjects;

namespace DevWiki.CleanArch.Domain.SharedKernel;

public sealed class Cpf
{
    private Cpf(string numero)
    {
        Numero = numero;
    }

    public string Numero { get; }

    public string Formatado =>
        $"{Numero[..3]}.{Numero[3..6]}.{Numero[6..9]}-{Numero[9..11]}";

    public static Result<Cpf> Criar(string? cpf)
    {
        if (cpf is null)
            return Result<Cpf>.Falha("CPF não pode ser nulo.");

        var digitos = new string([.. cpf.Where(char.IsDigit)]);

        if (digitos.Length != 11)
            return Result<Cpf>.Falha("CPF deve ter 11 dígitos.");

        if (digitos.Distinct().Count() == 1)
            return Result<Cpf>.Falha("CPF inválido.");

        if (!ValidarDigito(digitos, posicao: 9, pesoInicial: 10) ||
            !ValidarDigito(digitos, posicao: 10, pesoInicial: 11))
            return Result<Cpf>.Falha("CPF inválido.");

        return Result<Cpf>.Ok(new Cpf(digitos));
    }

    private static bool ValidarDigito(string digitos, int posicao, int pesoInicial)
    {
        int soma = 0;
        for (int i = 0; i < posicao; i++)
            soma += (digitos[i] - '0') * (pesoInicial - i);
        int resto = soma % 11;
        int esperado = resto < 2 ? 0 : 11 - resto;
        return (digitos[posicao] - '0') == esperado;
    }

    public override string ToString() => Formatado;

    public override bool Equals(object? obj) => obj is Cpf other && Numero == other.Numero;

    public override int GetHashCode() => Numero.GetHashCode();

    public static bool operator ==(Cpf? left, Cpf? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Cpf? left, Cpf? right) => !(left == right);
}

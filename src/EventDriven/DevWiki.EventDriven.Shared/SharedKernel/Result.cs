namespace DevWiki.EventDriven.Shared.SharedKernel;

public sealed class Result<T>
{
    public bool Sucesso { get; }
    public T? Valor { get; }
    public string? Erro { get; }

    private Result(bool sucesso, T? valor, string? erro)
    {
        Sucesso = sucesso;
        Valor = valor;
        Erro = erro;
    }

    public static Result<T> Ok(T valor) => new(true, valor, null);
    public static Result<T> Falha(string erro) => new(false, default, erro);

    public static implicit operator bool(Result<T> result) => result.Sucesso;
    public override string ToString() =>
        Sucesso ? $"Ok({Valor})" : $"Falha({Erro})";
}

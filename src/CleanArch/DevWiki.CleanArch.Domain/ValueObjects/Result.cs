namespace DevWiki.CleanArch.Domain.ValueObjects;

public sealed class Result<T>
{
    private Result() { }

    public bool Sucesso { get; private init; }
    public T? Valor { get; private init; }
    public string? MensagemErro { get; private init; }
    public IReadOnlyList<string> Erros { get; private init; } = [];

    public static Result<T> Ok(T valor)
        => new() { Sucesso = true, Valor = valor };

    public static Result<T> Falha(string mensagem)
        => new() { Sucesso = false, MensagemErro = mensagem, Erros = [mensagem] };

    public static Result<T> Falha(IReadOnlyList<string> erros)
        => new() { Sucesso = false, MensagemErro = string.Join("; ", erros), Erros = erros };
}
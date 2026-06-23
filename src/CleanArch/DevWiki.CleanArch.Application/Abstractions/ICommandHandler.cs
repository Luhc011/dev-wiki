using DevWiki.CleanArch.Domain.ValueObjects;

namespace DevWiki.CleanArch.Application.Abstractions;

public interface ICommandHandler<TCommand, TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct = default);
}
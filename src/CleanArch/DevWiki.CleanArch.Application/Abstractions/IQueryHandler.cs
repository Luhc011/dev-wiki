using DevWiki.CleanArch.Domain.ValueObjects;

namespace DevWiki.CleanArch.Application.Abstractions;

public interface IQueryHandler<TQuery, TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct = default);
}
using DevWiki.CleanArch.Domain.Entities;
using DevWiki.CleanArch.Domain.Enums;
using DevWiki.CleanArch.Domain.SharedKernel;

namespace DevWiki.CleanArch.Domain.Interfaces;

public interface IRepositorioCobranca
{
    Task<Cobranca?> BuscarPorIdAsync(CobrancaId id, CancellationToken ct = default);
    Task<IReadOnlyList<Cobranca>> ListarPorStatusAsync(StatusCobranca status, CancellationToken ct = default);
    Task SalvarAsync(Cobranca cobranca, CancellationToken ct = default);
    Task AtualizarAsync(Cobranca cobranca, CancellationToken ct = default);
}
using DevWiki.CleanArch.Domain.Enums;

namespace DevWiki.CleanArch.Application.UseCases.RegistrarPagamento;

public sealed record RegistrarPagamentoResult(Guid PagamentoId, StatusCobranca NovoStatus);
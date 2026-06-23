using DevWiki.CleanArch.Domain.Enums;
using DevWiki.CleanArch.Domain.SharedKernel;

namespace DevWiki.CleanArch.Application.UseCases.ProcessarCobranca;

public sealed record ProcessarCobrancaResult(CobrancaId CobrancaId, StatusCobranca Status);
using DevWiki.CleanArch.Application.Abstractions;
using DevWiki.CleanArch.Domain.Interfaces;
using DevWiki.CleanArch.Domain.SharedKernel;
using DevWiki.CleanArch.Domain.ValueObjects;

namespace DevWiki.CleanArch.Application.UseCases.RegistrarPagamento;

public sealed class RegistrarPagamentoHandler(IRepositorioCobranca repositorio, INotificadorCobranca notificador)
    : ICommandHandler<RegistrarPagamentoCommand, RegistrarPagamentoResult>
{
    public async Task<Result<RegistrarPagamentoResult>> HandleAsync(RegistrarPagamentoCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));

        var cobrancaId = CobrancaId.De(command.CobrancaId);
        var cobranca = await repositorio.BuscarPorIdAsync(cobrancaId, ct);

        if (cobranca is null)
            return Result<RegistrarPagamentoResult>.Falha($"ccbranca com id '{cobrancaId}' n encontrada");

        var valorResult = Valor.Criar(command.ValorPago);
        if (!valorResult.Sucesso)
            return Result<RegistrarPagamentoResult>.Falha(valorResult.MensagemErro!);

        var pagamentoResult = cobranca.RegistrarPagamento(valorResult.Valor!, command.DataHora);
        if (!pagamentoResult.Sucesso)
            return Result<RegistrarPagamentoResult>.Falha(pagamentoResult.MensagemErro!);

        await repositorio.AtualizarAsync(cobranca, ct);
        await notificador.NotificarAsync(cobranca.CpfDevedor.Formatado, $"pagamento de {command.ValorPago:F2} registrado", ct);

        return Result<RegistrarPagamentoResult>.Ok(new RegistrarPagamentoResult(pagamentoResult.Valor!.Id, cobranca.Status));
    }
}
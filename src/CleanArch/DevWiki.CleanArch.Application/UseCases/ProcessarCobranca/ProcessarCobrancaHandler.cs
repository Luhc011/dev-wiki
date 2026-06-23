using DevWiki.CleanArch.Application.Abstractions;
using DevWiki.CleanArch.Domain.Interfaces;
using DevWiki.CleanArch.Domain.SharedKernel;
using DevWiki.CleanArch.Domain.ValueObjects;

namespace DevWiki.CleanArch.Application.UseCases.ProcessarCobranca;

public sealed class ProcessarCobrancaHandler(IRepositorioCobranca repositorio, INotificadorCobranca notificador)
    : ICommandHandler<ProcessarCobrancaCommand, ProcessarCobrancaResult>
{
    public async Task<Result<ProcessarCobrancaResult>> HandleAsync(ProcessarCobrancaCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));

        var cpfResult = Cpf.Criar(command.CpfDevedor);
        if (!cpfResult.Sucesso)
            return Result<ProcessarCobrancaResult>.Falha(cpfResult.MensagemErro!);

        var valorResult = Valor.Criar(command.ValorTotal);
        if (!valorResult.Sucesso)
            return Result<ProcessarCobrancaResult>.Falha(valorResult.MensagemErro!);

        var cobranca = Domain.Entities.Cobranca.Criar(
            cpfResult.Valor!, valorResult.Valor!, command.Descricao, command.DataVencimento);

        await repositorio.SalvarAsync(cobranca, ct);
        await notificador.NotificarAsync(
            cpfResult.Valor!.Formatado,
            $"cobranca de R$ {command.ValorTotal:F2} registrada. vencimento: {command.DataVencimento:dd/MM/yyyy}",
            ct);

        return Result<ProcessarCobrancaResult>.Ok(new ProcessarCobrancaResult(cobranca.CobrancaId, cobranca.Status));
    }
}
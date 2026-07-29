using DevWiki.EventDriven.EventSourcing.Events;
using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EventSourcing.Projections;

public sealed class ExtratoBancarioProjection
{
    public Guid CobrancaId { get; private set; }
    public string CpfDevedor { get; private set; } = string.Empty;
    public decimal ValorOriginal { get; private set; }
    public decimal? ValorPago { get; private set; }
    public string StatusAtual { get; private set; } = string.Empty;
    public DateTimeOffset? PagoEm { get; private set; }
    public List<string> Historico { get; } = [];

    public void Aplicar(AggregateEvent evento)
    {
        switch (evento)
        {
            case CobrancaCriadaEvent e:
                CobrancaId = e.AggregateId;
                CpfDevedor = e.CpfDevedor;
                ValorOriginal = e.Valor;
                StatusAtual = "Pendente";
                Historico.Add($"[v{e.Versao}] Cobrança criada — R$ {e.Valor:F2} para CPF {e.CpfDevedor}");
                break;

            case PagamentoRegistradoEvent e:
                ValorPago = e.ValorPago;
                PagoEm = e.PagoEm;
                StatusAtual = "Paga";
                Historico.Add($"[v{e.Versao}] Pagamento registrado — R$ {e.ValorPago:F2} em {e.PagoEm:dd/MM/yyyy}");
                break;

            case CobrancaCanceladaEvent e:
                StatusAtual = "Cancelada";
                Historico.Add($"[v{e.Versao}] Cobrança cancelada — motivo: {e.Motivo}");
                break;

            case CobrancaVencidaEvent e:
                StatusAtual = "Vencida";
                Historico.Add($"[v{e.Versao}] Cobrança vencida em {e.VencidaEm:dd/MM/yyyy}");
                break;
        }
    }

    public static ExtratoBancarioProjection Construir(IEnumerable<AggregateEvent> eventos)
    {
        var projecao = new ExtratoBancarioProjection();
        foreach (var evento in eventos)
            projecao.Aplicar(evento);
        return projecao;
    }
}

using DevWiki.EventDriven.EventSourcing.Events;
using DevWiki.EventDriven.EventSourcing.Snapshots;
using DevWiki.EventDriven.Shared.Domain;
using DevWiki.EventDriven.Shared.Events;

namespace DevWiki.EventDriven.EventSourcing.Aggregates;

public sealed class CobrancaAggregate
{
    public Guid Id { get; private set; }
    public string CpfDevedor { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }
    public DateTimeOffset DataVencimento { get; private set; }
    public StatusCobranca Status { get; private set; }
    public long Versao { get; private set; }

    private readonly List<AggregateEvent> _eventosPendentes = [];

    public IReadOnlyList<AggregateEvent> EventosPendentes => [.. _eventosPendentes];

    public CobrancaAggregate() { }

    public static CobrancaAggregate Criar(string cpfDevedor, decimal valor, DateTimeOffset dataVencimento)
    {
        var aggregate = new CobrancaAggregate();
        aggregate.AplicarERegistrar(new CobrancaCriadaEvent
        {
            AggregateId = Guid.NewGuid(),
            Versao = 1,
            CpfDevedor = cpfDevedor,
            Valor = valor,
            DataVencimento = dataVencimento
        });
        return aggregate;
    }

    public static CobrancaAggregate Reconstituir(IEnumerable<AggregateEvent> eventos)
    {
        var aggregate = new CobrancaAggregate();
        foreach (var evento in eventos)
            aggregate.Aplicar(evento);
        return aggregate;
    }

    public static CobrancaAggregate ReconstituirDeSnapshot(Snapshot snapshot, IEnumerable<AggregateEvent> eventosPosSnapshot)
    {
        var partes = snapshot.Estado.Split('|');
        var aggregate = new CobrancaAggregate
        {
            Id = snapshot.AggregateId,
            CpfDevedor = partes[0],
            Valor = decimal.Parse(partes[1]),
            DataVencimento = DateTimeOffset.Parse(partes[2]),
            Status = Enum.Parse<StatusCobranca>(partes[3]),
            Versao = snapshot.Versao
        };

        foreach (var evento in eventosPosSnapshot)
            aggregate.Aplicar(evento);

        return aggregate;
    }

    //Comandos
    public void RegistrarPagamento(decimal valorPago)
    {
        if (Status is not StatusCobranca.Pendente)
            throw new InvalidOperationException($"Cobrança {Id} não está pendente (status: {Status})");

        AplicarERegistrar(new PagamentoRegistradoEvent
        {
            AggregateId = Id,
            Versao = Versao + 1,
            ValorPago = valorPago,
            PagoEm = DateTimeOffset.UtcNow
        });
    }

    public void Cancelar(string motivo)
    {
        if (Status is not StatusCobranca.Cancelada)
            throw new InvalidOperationException($"cobranca {Id} ja esta em estado final ({Status})");

        AplicarERegistrar(new CobrancaCanceladaEvent
        {
            AggregateId = Id,
            Versao = Versao + 1,
            Motivo = motivo,
            CanceladoEm = DateTimeOffset.UtcNow
        });
    }

    public void Vencer()
    {
        if (Status is not StatusCobranca.Pendente)
            throw new InvalidOperationException($"cobranca {Id} nao esta pendente");

        AplicarERegistrar(new CobrancaVencidaEvent
        {
            AggregateId = Id,
            Versao = Versao + 1,
            VencidaEm = DateTimeOffset.UtcNow
        });
    }

    public string SerializarEstado() => $"{CpfDevedor}|{Valor}|{DataVencimento:O}|{Status}";

    private void AplicarERegistrar(AggregateEvent evento)
    {
        Aplicar(evento);
        _eventosPendentes.Add(evento);
    }

    private void Aplicar(AggregateEvent evento)
    {
        switch (evento)
        {
            case CobrancaCriadaEvent e:
                Id = e.AggregateId;
                CpfDevedor = e.CpfDevedor;
                Valor = e.Valor;
                DataVencimento = e.DataVencimento;
                Status = StatusCobranca.Pendente;
                break;

            case PagamentoRegistradoEvent:
                Status = StatusCobranca.Paga;
                break;

            case CobrancaCanceladaEvent:
                Status = StatusCobranca.Cancelada;
                break;

            case CobrancaVencidaEvent:
                Status = StatusCobranca.Vencida;
                break;
        }

        Versao = evento.Versao;
    }
}

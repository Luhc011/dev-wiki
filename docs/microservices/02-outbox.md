# Outbox Pattern

## 1. O problema Dual Write

### O cenário

Toda operação em microsserviços tem uma dualidade inevitável:
1. Gravar no banco de dados (o estado do serviço)
2. Publicar uma mensagem no broker (informar outros serviços)

```csharp
// Parece simples, mas é um problema:
public async Task CriarCobrancaAsync(string cpf, decimal valor)
{
    await _banco.SalvarCobrancaAsync(new Cobranca(cpf, valor));  // Passo 1
    await _broker.PublicarAsync(new CobrancaCriadaEvento(...));   // Passo 2
}
```

### Por que isso é problemático

**Cenário A**: Banco salva, broker cai

```
Passo 1: ✅ Cobrança salva no banco
Passo 2: ❌ Broker indisponível — evento nunca publicado

Resultado: ServiçoAntifraude nunca soube que a cobrança foi criada.
           Saga nunca iniciou. Sistema inconsistente.
```

**Cenário B**: Evento publicado, banco falha

```
Passo 1: ❌ Banco falhou durante o save
Passo 2: ✅ Evento já publicado no broker

Resultado: Outros serviços estão processando uma cobrança que não existe.
           Sistema inconsistente.
```

### Por que não usar transação entre banco e broker

- A maioria dos brokers (Kafka, RabbitMQ) não suporta XA transactions
- XA transactions são lentas e complexas
- O broker pode não ter suporte a 2PC

---

## 2. A solução: Outbox Pattern

### A ideia central

Em vez de publicar no broker diretamente, **grave o evento na tabela Outbox**
dentro da mesma transação do banco. Um Worker separado lê a tabela Outbox e
publica no broker quando o broker está disponível.

### A tabela Outbox

```sql
CREATE TABLE outbox_eventos (
    id              UUID PRIMARY KEY,
    nome_evento     VARCHAR(100) NOT NULL,
    payload         TEXT NOT NULL,           -- serialização JSON do evento
    criado_em       TIMESTAMPTZ NOT NULL,
    processado_em   TIMESTAMPTZ NULL,        -- NULL = pendente
    tentativas      INTEGER DEFAULT 0
);
```

### O fluxo com Outbox

```
╔══════════════════════════════════════════════════════╗
║  TRANSAÇÃO LOCAL (banco único)                       ║
║                                                      ║
║  1. INSERT INTO cobranças (...);                     ║
║  2. INSERT INTO outbox_eventos (nome, payload, ...); ║
║                                                      ║
║  COMMIT; ← ambos salvos atomicamente                 ║
╚══════════════════════════════════════════════════════╝
                    │
                    │ mais tarde (segundos)
                    ▼
╔══════════════════════════════════════════════════════╗
║  OUTBOX WORKER (processo separado)                   ║
║                                                      ║
║  3. SELECT * FROM outbox_eventos WHERE processado_em ║
║     IS NULL LIMIT 50;                               ║
║                                                      ║
║  4. Para cada evento:                                ║
║     a. publicar no broker (Kafka/RabbitMQ)          ║
║     b. se sucesso: UPDATE outbox SET                 ║
║           processado_em = NOW() WHERE id = ?;       ║
║     c. se falha: UPDATE outbox SET                   ║
║           tentativas = tentativas + 1 WHERE id = ?; ║
║        (tenta na próxima rodada)                    ║
╚══════════════════════════════════════════════════════╝
```

### Por que isso resolve o problema

- Banco e Outbox: mesma transação → atômico ✅
- Outbox → Broker: se o broker cair, o Worker tenta novamente depois ✅
- Se o Worker cair entre publicar e atualizar: o evento é publicado novamente ✅
  (lidar com duplicatas é responsabilidade do consumidor — Inbox Pattern)

---

## 3. Garantias do Outbox

### At-least-once delivery

O Outbox garante que o evento **será publicado pelo menos uma vez**. Não garante
exatamente uma vez (exactly-once) porque:

```
1. Worker publica evento no broker → sucesso ✅
2. Worker chama UPDATE processado_em = NOW() → 
   FALHA (worker caiu, rede particionou)
3. Worker reinicia
4. Vê o evento ainda como pendente (processado_em IS NULL)
5. Publica de novo no broker → duplicata!
```

Isso é chamado de **at-least-once delivery** — inevitável sem exactly-once
semantics no broker (que Kafka oferece como garantia extra).

### Como lidar com duplicatas: Inbox Pattern

O **Inbox Pattern** é o par do Outbox — garante idempotência no lado do **consumidor**.

---

## 4. Inbox Pattern

### O problema do consumidor

O consumidor pode receber a mesma mensagem mais de uma vez:
- At-least-once delivery do broker
- Worker que publicou antes de marcar como processado

### A solução

Cada mensagem tem um `MensagemId` único. O consumidor registra na tabela Inbox
os IDs de mensagens que já processou.

```sql
CREATE TABLE inbox_eventos (
    mensagem_id     UUID PRIMARY KEY,  -- ID único da mensagem do broker
    nome_evento     VARCHAR(100),
    recebido_em     TIMESTAMPTZ,
    processado_em   TIMESTAMPTZ
);
```

### Fluxo do InboxProcessor

```
Mensagem recebida do broker (mensagemId = "abc-123")
      │
      ▼
JaProcessado("abc-123")? ──── SIM ──→ Ignorar (idempotente)
      │
      NÃO
      │
      ▼
Executar processamento()
      │
      ▼
Registrar("abc-123") na tabela Inbox
      │
      ▼
Confirmar recebimento ao broker
```

Se o processamento cair entre "Executar" e "Registrar", na próxima tentativa
o processamento é reexecutado. Por isso o **processamento também deve ser idempotente**.

---

## 5. Variações e considerações práticas

### Polling vs Change Data Capture (CDC)

**Polling** (implementado neste módulo):
```
Worker: SELECT eventos pendentes a cada 5 segundos
```
- Simples de implementar
- Latência de até N segundos (frequência do worker)
- Gera carga constante no banco

**CDC (Change Data Capture)**:
```
Debezium monitora o WAL (Write-Ahead Log) do PostgreSQL
→ Publica no Kafka imediatamente quando Outbox tem novo registro
```
- Menor latência
- Sem polling — reativo
- Mais complexo de configurar

### Frequência do worker

```csharp
// Frequente (1s) → baixa latência, mais carga no banco
// Raro (60s) → menor carga, maior latência
// Trade-off: frequência depende do SLA de entrega de eventos
var intervalo = TimeSpan.FromSeconds(5); // equilíbrio razoável
```

### Cleanup de eventos antigos

```sql
-- Após N dias, deletar eventos já processados
DELETE FROM outbox_eventos
WHERE processado_em IS NOT NULL
  AND processado_em < NOW() - INTERVAL '7 days';
```

### OutboxEvento.TentativasProcessamento

Se um evento falha repetidamente, pode indicar problema estrutural (payload inválido,
consumidor não existe). Usar o contador de tentativas para:
- Alertar após 5 tentativas
- Mover para Dead Letter Queue após 10 tentativas
- Não tentar infinitamente (evita thundering herd)

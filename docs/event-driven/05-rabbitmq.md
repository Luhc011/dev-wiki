# RabbitMQ — Message Broker com AMQP

> **Pré-requisito**: [Pub/Sub](./03-pub-sub.md)  
> **Próximo documento**: [Padrões de Confiabilidade](./06-reliability-patterns.md)

---

## O que é o RabbitMQ?

RabbitMQ é um **message broker** baseado no protocolo AMQP (Advanced Message Queuing Protocol).
O modelo central é: producers publicam mensagens em **exchanges**, exchanges as roteiam
para **queues** baseado em regras, consumers consomem das queues.

Diferente do Kafka (log distribuído), no RabbitMQ uma mensagem **confirmada pelo consumer**
é removida da fila. Não há replay nativo.

Essa diferença de modelo implica casos de uso diferentes — nenhum dos dois é "melhor".
São ferramentas com modelos distintos que resolvem problemas distintos.

---

## Arquitetura — os componentes

```
Producer
   │
   │ publish(exchange="pagamentos", routingKey="pagamento.pix.processado", payload=...)
   ▼
Exchange
   │
   │ aplica regras de roteamento (binding + routing key)
   ├──► Queue "pix-processador"    ──► Consumer A
   ├──► Queue "auditoria"          ──► Consumer B
   └──► Queue "notificacoes"       ──► Consumer C
```

### Exchange

Recebe mensagens do producer e as roteia para filas baseado em regras.
**Não armazena mensagens** — apenas roteia. Se nenhuma fila está vinculada à exchange,
a mensagem é descartada.

### Queue

Armazena mensagens até serem consumidas. Com ACK: removida. Com NACK: requeue ou DLQ.
A durabilidade (memória vs disco) é configurável por fila.

### Binding

Vínculo entre Exchange e Queue com uma routing key (padrão de roteamento). Define
a regra: "mensagens que chegam com este padrão de routing key vão para esta fila".

### Consumer

Lê mensagens da queue. Deve responder com **ACK** (processou com sucesso) ou
**NACK** (falha — requeue ou DLQ).

---

## Os quatro tipos de Exchange

### Direct Exchange — roteamento exato

Roteia para queues cuja **binding key = routing key** exata da mensagem.

```
Producer: publish(routingKey="pagamento.pix")
         │
         ▼
Exchange [direct: pagamentos]
         │
         ├── Binding key "pagamento.pix"   ──► Queue "pix-proc"   ──► Consumer A
         ├── Binding key "pagamento.ted"   ──► Queue "ted-proc"   ──► Consumer B
         └── Binding key "pagamento.boleto"──► Queue "boleto-proc"──► Consumer C

Resultado: apenas "pix-proc" recebe (match exato)
```

**Caso de uso**: roteamento por tipo exato de evento, sem ambiguidade.

### Fanout Exchange — broadcast

Envia para **todas as queues** vinculadas. A routing key é ignorada.

```
Producer: publish(routingKey="qualquer")   ← routing key ignorada
         │
         ▼
Exchange [fanout: broadcast]
         │
         ├──────────────────────► Queue "notificacoes"  ──► Consumer A
         ├──────────────────────► Queue "auditoria"     ──► Consumer B
         └──────────────────────► Queue "extrato"       ──► Consumer C

Resultado: TODAS as queues recebem a mensagem
```

**Caso de uso**: evento que todos os serviços precisam receber (ex.: nova política de tarifas).

### Topic Exchange — wildcards

Roteia baseado em **padrões** na routing key com wildcards `*` e `#`.

```
Producer A: publish(routingKey="pagamento.pix.processado")
Producer B: publish(routingKey="pagamento.ted.cancelado")
Producer C: publish(routingKey="fraude.detectada")
         │
         ▼
Exchange [topic: eventos]
         │
         ├── Binding "pagamento.#"            ──► Queue "todos-pagamentos"
         │   ✓ "pagamento.pix.processado"
         │   ✓ "pagamento.ted.cancelado"
         │   ✗ "fraude.detectada"
         │
         ├── Binding "pagamento.*.processado" ──► Queue "pagamentos-processados"
         │   ✓ "pagamento.pix.processado"   (* = "pix")
         │   ✗ "pagamento.ted.cancelado"    (não termina em .processado)
         │   ✗ "pagamento.processado"       (* exige exatamente 1 palavra no meio)
         │
         └── Binding "*.detectada"            ──► Queue "alertas"
             ✓ "fraude.detectada"           (* = "fraude")
             ✗ "pagamento.pix.processado"   (* = exatamente 1 palavra)
```

**Wildcards:**
- `*` substitui **exatamente uma palavra** entre pontos
- `#` substitui **zero ou mais palavras**

**Caso de uso**: roteamento flexível por hierarquia de routing keys.

### Headers Exchange — filtro por headers

Roteia baseado nos **headers** da mensagem (metadados separados do payload), não
na routing key. Mais flexível e mais custoso que Topic Exchange.

```
Producer: publish(
  headers={"tipo": "pagamento", "valor-acima-de": "1000", "banco": "itau"},
  payload=...
)
         │
         ▼
Exchange [headers: filtrado]
         │
         ├── Binding {tipo=pagamento, x-match=all} ──► Queue "todos-pagamentos"
         │   x-match=all: TODOS os headers precisam bater
         │
         └── Binding {banco=itau, x-match=any}     ──► Queue "monitoramento-itau"
             x-match=any: PELO MENOS UM header precisa bater
```

**`x-match=all`**: equivale a AND — todos os headers definidos no binding precisam bater.  
**`x-match=any`**: equivale a OR — qualquer um dos headers definidos no binding é suficiente.

**Nota**: Headers Exchange inspeciona metadados da mensagem, não o conteúdo do payload.
Para filtro real por payload, a lógica precisa estar no consumer.

---

## ACK e NACK — o protocolo de confiabilidade

O ACK/NACK é o mecanismo que garante at-least-once delivery no RabbitMQ.

```csharp
var mensagem = await fila.DesenfileirarAsync();

try
{
    await ProcessarPagamento(mensagem.Payload);
    await fila.AckAsync(mensagem.Id);         // ✓ removida da fila
}
catch (CpfInvalidoException)
{
    // Dado irrecuperável — não adianta tentar de novo
    await fila.NackAsync(mensagem.Id, requeue: false);   // → DLQ (se configurada)
}
catch (ServicoTemporarioException)
{
    // Erro transitório — tentar novamente
    await fila.NackAsync(mensagem.Id, requeue: true);    // → volta para a fila
}
```

**Se o consumer cair antes do ACK**: a mensagem é reentregue automaticamente ao próximo
consumer disponível. Isso garante at-least-once: pode processar mais de uma vez, mas
nunca perde.

**Por isso idempotência é obrigatória**: um mesmo pagamento pode ser processado duas vezes
em caso de falha. Veja [Padrões de Confiabilidade](./06-reliability-patterns.md) para
o Inbox Pattern.

---

## Prefetch (QoS) — controle de backpressure

**Prefetch** define quantas mensagens o broker pode enviar ao consumer antes de receber
ACK. É o mecanismo de backpressure do RabbitMQ.

```
Sem prefetch (padrão):
  Broker envia TODAS as mensagens disponíveis para o consumer de uma vez
  Consumer lento acumula mensagens em memória local → pode esgotar memória

Com prefetch=10:
  Broker envia no máximo 10 mensagens simultaneamente por consumer
  Depois de ACK, broker envia mais 1 (mantendo o limite)
  Consumer processa no seu ritmo sem sobrecarregar

Prefetch muito alto: consumer sobrecarregado, outras instâncias ociosas
Prefetch muito baixo: throughput baixo (consumer fica esperando mais mensagens)
```

```csharp
// Configurar prefetch de 10 mensagens por consumer
channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
```

Valor típico: entre 10 e 100, dependendo do tempo de processamento por mensagem.
Para processamento rápido (< 1ms), valores altos (100+). Para processamento lento
(> 100ms), valores baixos (5-20).

---

## Dead Letter Queue (DLQ)

A DLQ é o destino de mensagens que não puderam ser processadas. Evita que mensagens
problemáticas fiquem em loop infinito de retry ou sejam perdidas silenciosamente.

```
1. Consumer tenta processar mensagem de pagamento
2. Falha: CPF inválido (dado irrecuperável)
3. NACK com requeue=false → mensagem vai para DLQ "pagamentos.dlq"

Ou com retry automático:
1. Consumer tenta processar
2. Falha transitória: serviço downstream indisponível
3. NACK com requeue=true → volta para a fila principal
4. (após MaxTentativas = 3) → NACK final → vai para DLQ
```

### Cenários que enviam para DLQ

| Cenário | Mecanismo |
|---|---|
| Consumer rejeita sem requeue | `basicNack(requeue=false)` |
| TTL da mensagem expirou | `x-message-ttl` na fila |
| Fila atingiu capacidade máxima | `x-max-length` na fila |
| Máximo de tentativas atingido | Implementação customizada via `x-death` header |

### O que fazer com mensagens na DLQ

- Monitorar: alertas quando DLQ cresce
- Inspecionar: entender por que mensagens estão falhando
- Corrigir e reprocessar: após correção do bug, publicar novamente na fila original
- Descartar: para dados genuinamente inválidos sem chance de recuperação

---

## Competing Consumers com RabbitMQ

Múltiplos consumers na mesma fila automaticamente compete pelas mensagens:

```
Queue "processamento-pagamentos" com 100 mensagens pendentes:

  Consumer A ──► mensagem 1
  Consumer B ──► mensagem 2
  Consumer C ──► mensagem 3
  (processando em paralelo)

Se Consumer B cai:
  → Mensagens sem ACK são redistribuídas para A e C
  → Nenhuma mensagem perdida
```

---

## RabbitMQ vs Kafka — comparativo completo

| Aspecto | RabbitMQ | Kafka |
|---|---|---|
| **Modelo** | Message broker — exchange + queue | Log distribuído — topic + partition |
| **Mensagem consumida** | Removida da fila | Permanece no log |
| **Replay** | Sem suporte nativo | Natural via reset de offset |
| **Throughput** | Dezenas a centenas de milhares/s (hardware dependente) | Milhões/s por partition |
| **Roteamento** | Flexível: Direct, Fanout, Topic, Headers | Simples: topic + partition key |
| **Ordenação** | FIFO por fila | Garantida por partition |
| **DLQ** | Nativo e configurável | Requer implementação customizada |
| **Backpressure** | Prefetch count nativo | Consumer lag + auto-scaling |
| **RPC (request-reply)** | Suportado nativamente | Não é caso de uso natural |
| **Múltiplos consumers** | Competing consumers (mesma fila) | Consumer groups independentes |
| **Persistência** | Configurável (memória ou disco) | Sempre em disco (durável por design) |
| **Complexidade operacional** | Média (exchanges, bindings, vhosts) | Alta (cluster, partitions, replication, schema registry) |
| **Ideal para** | Filas de tarefas, RPC, roteamento complexo, DLQ sofisticada | Event streaming, replay, múltiplos consumer groups, alto volume |

### Notas sobre throughput

Os números de throughput são **altamente dependentes de hardware, configuração e
padrão de uso**. Não use esses valores como referência absoluta. A diferença relevante
é de escala: Kafka foi projetado para volumes que fariam um cluster RabbitMQ médio
entrar em colapso. Para volumes menores (centenas de milhares por segundo), RabbitMQ
frequentemente é suficiente e mais simples de operar.

---

## Quando usar RabbitMQ

- **Roteamento flexível e complexo**: exchanges com bindings por routing key, wildcards, headers
- **Request-Reply (RPC)**: padrão `reply-to` nativo para comunicação síncrona over messaging
- **Filas de tarefas (job queues)**: jobs distribuídos entre workers, com DLQ para falhas
- **Processamento com TTL**: mensagens que expiram se não processadas em N segundos
- **Integração com sistemas legados**: AMQP é um protocolo padronizado, mais compatível com sistemas antigos
- **Equipes que precisam de operação mais simples**: menos componentes do que um cluster Kafka completo

---

## Quando NÃO usar RabbitMQ

- **Replay histórico**: mensagem consumida é removida — sem possibilidade de reprocessar histórico
- **Múltiplos consumer groups independentes**: possível, mas não é o modelo natural do RabbitMQ
- **Volume muito alto e sustentado**: para milhões de mensagens por segundo com retenção, Kafka é mais adequado
- **Event Sourcing como log**: o modelo de log imutável do Kafka é mais natural para event sourcing de larga escala

---

## Resumo

- RabbitMQ usa um modelo de **exchange + binding + queue** — mais flexível para roteamento
- Os quatro tipos de exchange (Direct, Fanout, Topic, Headers) cobrem a maioria dos cenários de roteamento
- **ACK/NACK** garante at-least-once delivery — idempotência é necessária no consumer
- **Prefetch** é o mecanismo de backpressure — define quantas mensagens chegam ao consumer de uma vez
- **DLQ** evita perda de mensagens que falham repetidamente — monitorar DLQ é obrigatório em produção
- RabbitMQ e Kafka não são concorrentes diretos: modelos diferentes, casos de uso diferentes

---

## Perguntas para fixação

1. Qual a diferença entre Exchange e Queue no RabbitMQ?
2. Para que serve o Fanout Exchange? Quando você o usaria?
3. O que acontece com uma mensagem quando o consumer faz NACK com requeue=false?
4. O que é prefetch e por que ele importa?
5. Quando uma mensagem vai para a DLQ? O que você deve fazer com mensagens lá?
6. Qual a diferença principal de modelo entre RabbitMQ e Kafka?

---

## Perguntas de entrevista

**Iniciante**

> O que é o RabbitMQ?

RabbitMQ é um message broker baseado em AMQP. Producers publicam mensagens em exchanges,
que as roteiam para queues baseado em regras (bindings). Consumers processam das queues.
A mensagem confirmada (ACK) é removida. Se o consumer falha, a mensagem é reentregue.

> O que é ACK e NACK?

ACK (acknowledgment): consumer sinaliza que processou a mensagem com sucesso — ela é
removida da fila. NACK (negative acknowledgment): consumer sinaliza falha. Com
`requeue=true`, a mensagem volta para a fila para ser tentada novamente. Com
`requeue=false`, vai para a DLQ (se configurada).

**Intermediário**

> Qual a diferença entre Direct, Fanout e Topic Exchange?

Direct: routing key deve ser igual à binding key — roteamento exato para uma fila específica.
Fanout: ignora routing key — todas as filas vinculadas recebem a mensagem (broadcast).
Topic: routing key tem padrão com wildcards (* = uma palavra, # = zero ou mais) —
roteamento flexível por hierarquia.

> O que é prefetch e qual o risco de não configurar?

Prefetch define quantas mensagens o broker envia antes de receber ACK. Sem prefetch,
o broker pode enviar todas as mensagens disponíveis para um consumer de uma vez, causando
sobrecarga de memória no consumer enquanto outros ficam ociosos. Com prefetch configurado,
a carga é distribuída de forma controlada entre consumers.

**Avançado**

> Como você implementaria retry com backoff exponencial usando RabbitMQ?

Sem retry nativo no RabbitMQ, usa-se uma estratégia com Dead Letter Exchanges:
(1) Fila principal com x-dead-letter-exchange configurado. (2) Na falha, NACK com
requeue=false — mensagem vai para fila de espera. (3) Fila de espera tem x-message-ttl
crescente (1s, 5s, 30s — backoff exponencial) e dead-letter-exchange apontando para
a fila principal. (4) Após TTL, a mensagem "morre" da fila de espera e volta para
a fila principal via DLX. (5) Após N tentativas (rastreado via x-death header),
vai para DLQ permanente.

---

## Relação com outros documentos

- [Pub/Sub](./03-pub-sub.md) — RabbitMQ implementa o padrão Pub/Sub com exchanges e bindings
- [Kafka](./04-kafka.md) — comparativo de modelo, casos de uso e trade-offs
- [Padrões de Confiabilidade](./06-reliability-patterns.md) — Inbox Pattern para idempotência com RabbitMQ; Outbox para publicação atômica
# Apache Kafka — Log Distribuído para Event Streaming

> **Pré-requisito**: [Pub/Sub](./03-pub-sub.md)  
> **Próximo documento**: [RabbitMQ](./05-rabbitmq.md)

---

## O que é o Apache Kafka?

Kafka é um **log distribuído e durável**. Não é uma fila de mensagens no sentido
tradicional — a distinção importa e afeta todas as decisões de design.

| Fila tradicional (ex.: RabbitMQ) | Log distribuído (Kafka) |
|---|---|
| Mensagem consumida = **removida** da fila | Mensagem consumida = **permanece** no log |
| Consumer avança consumindo mensagens | Consumer avança seu **offset** no log |
| Múltiplos consumers **competem** pela mensagem | Múltiplos consumer groups **independentes** lêem o mesmo log |
| Replay impossível | Replay natural: resetar offset para posição anterior |

Criado pelo LinkedIn para lidar com bilhões de eventos por dia. Hoje está presente
em praticamente todos os sistemas financeiros de grande escala — bancos, fintechs,
bolsas de valores, processadores de pagamento.

---

## Kafka é fila ou Pub/Sub?

Essa pergunta é uma simplificação — a resposta depende de **como você usa**.

Com um único consumer group, Kafka se comporta como **fila com competing consumers**:
as partições são distribuídas entre as instâncias e cada mensagem é processada por uma.

Com múltiplos consumer groups independentes, Kafka se comporta como **Pub/Sub fan-out**:
cada grupo lê o log de forma independente e cada um recebe todas as mensagens.

```
Topic "pagamentos" com 3 partitions:

Consumer Group "notificacoes" (3 instâncias):
  → Instância A: partition 0   ← cada instância processa 1/3 das mensagens
  → Instância B: partition 1
  → Instância C: partition 2
  Comportamento: fila com competing consumers

Consumer Group "antifraude" (3 instâncias):
  → Lê as MESMAS mensagens de forma totalmente independente

Consumer Group "extrato" (1 instância):
  → Lê as MESMAS mensagens, processando tudo sozinha

Comportamento combinado: Pub/Sub entre groups + fila dentro de cada group
```

---

## Arquitetura — conceitos fundamentais

### Visão geral

```mermaid
graph TB
    subgraph Producers
        PA[Producer A\ncpf-529]
        PB[Producer B\ncpf-123]
    end

    subgraph Kafka Cluster
        subgraph "Topic: pagamentos (3 partitions, replication=2)"
            P0["Partition 0 (leader: Broker1)\n[0][1][2][3][4]..."]
            P1["Partition 1 (leader: Broker2)\n[0][1][2]..."]
            P2["Partition 2 (leader: Broker3)\n[0][1][2][3]..."]
        end
    end

    subgraph "Consumer Group: notificacoes"
        C1[Instância A\noffset P0=4]
        C2[Instância B\noffset P1=2]
        C3[Instância C\noffset P2=3]
    end

    PA -->|hash(cpf-529) → P0| P0
    PB -->|hash(cpf-123) → P1| P1
    P0 --> C1
    P1 --> C2
    P2 --> C3
```

### Topic

Um topic é um canal lógico de mensagens. Em termos técnicos, é uma **categoria** de
eventos. Você cria topics separados para tipos diferentes de evento:
`pagamentos`, `fraudes`, `notificacoes`.

### Partition

Cada topic é dividido em partitions — sequências ordenadas e imutáveis de mensagens.
Partitions são a unidade de paralelismo e escalabilidade do Kafka.

Cada partition é armazenada em um broker e pode ter réplicas em outros brokers.
O **leader** da partition é quem aceita leituras e escritas. As réplicas são **followers**
que replicam o log do leader.

### Offset

Cada mensagem numa partition tem um offset — um número sequencial que a identifica
dentro daquela partition. O consumer controla seu próprio progresso salvando o offset
da última mensagem processada.

```
Partition 0:  [offset 0] [offset 1] [offset 2] [offset 3] [offset 4]
                                                              ↑
                                            Consumer lendo offset 4
                                            Commit: "próxima = offset 5"
```

### Replication Factor

Cada partition tem um número configurável de réplicas. Com `replication-factor=3`:
- 1 leader (aceita leitura e escrita)
- 2 followers (replicam o log)

Se o broker com o leader cai, um follower é promovido automaticamente. A mensagem
só é considerada confirmada quando `min.insync.replicas` réplicas a receberam.

---

## Partition Key — como e por que usar

A partition key é um campo da mensagem usado para determinar em qual partition ela vai.
O algoritmo é um **hash consistente** (murmur2 no cliente Java/Confluent):

```
partition = murmur2(key) % totalPartitions
```

**Sem key (round-robin)**:
```
Mensagem 1 → Partition 0
Mensagem 2 → Partition 1
Mensagem 3 → Partition 2
Mensagem 4 → Partition 0  (volta ao início)
→ Carga distribuída, mas sem garantia de ordem entre partitions
```

**Com key (hash consistente)**:
```
key = "cobranca-abc-123"
→ Sempre Partition 0 (resultado do hash é determinístico)

Partition 0: [CobrancaCriada][CobrancaVencida][PagamentoRegistrado]
→ Todos os eventos desta cobrança chegam ao mesmo consumer em ordem
```

### Regra prática

Use o **ID do aggregate** como partition key quando a ordem entre eventos do mesmo
aggregate importa (Event Sourcing com Kafka, por exemplo).

Use **round-robin** (sem key) quando os eventos são independentes e você quer
distribuição uniforme de carga.

### Cuidado com hot partitions

```
✗ Problema: key = "tipo-pagamento" com valor "pix" para 90% das mensagens
  → Partition 0 recebe 90% da carga — "hot partition"
  → Outras partitions ficam ociosas — paralelismo desperdiçado

✓ Correto: key = "id-cobranca" → chaves distribuídas uniformemente
```

---

## Consumer Groups e paralelismo

```
Topic "pagamentos": 6 partitions

Cenário 1: 6 consumers (ideal — 1:1)
  Consumer 1 → Partition 0     → paralelismo máximo, cada consumer processa 1/6
  Consumer 2 → Partition 1
  Consumer 3 → Partition 2
  Consumer 4 → Partition 3
  Consumer 5 → Partition 4
  Consumer 6 → Partition 5

Cenário 2: 3 consumers (subótimo — 1:2)
  Consumer 1 → Partitions 0, 1   → cada consumer processa 2 partitions sequencialmente
  Consumer 2 → Partitions 2, 3
  Consumer 3 → Partitions 4, 5

Cenário 3: 8 consumers (desperdiçado)
  Consumers 1-6 → Partitions 0-5
  Consumer 7 → OCIOSO
  Consumer 8 → OCIOSO
```

**Regra**: `max(paralelismo útil) = min(consumers, partitions)`

Definir o número correto de partitions na criação do topic é importante: aumentar
partitions depois é possível mas pode quebrar a garantia de ordenação por key
(o hash de uma key muda de partition se o total muda).

### Rebalance

Quando uma instância entra ou sai do consumer group, ocorre um **rebalance**: as partitions
são redistribuídas entre as instâncias disponíveis. Durante o rebalance, o consumo para.

Em sistemas com alto volume, rebalances frequentes causam pausas visíveis. Estratégias
como **Cooperative Sticky Assignor** reduzem o impacto mantendo assignments existentes
e só movendo o necessário.

---

## Commit de Offsets — semânticas de entrega

O commit do offset é o que determina as garantias de entrega:

```
Log:     [msg0] [msg1] [msg2] [msg3] [msg4]
                              ↑ consumer lendo msg2, offset atual = 2

AT-MOST-ONCE (commit antes de processar):
  1. Consumer lê msg2
  2. Consumer commita offset=3
  3. Consumer processa msg2 ✓
  Se o consumer cair entre 2 e 3: msg2 foi perdida (offset já avançou)
  Resultado: pode perder, nunca duplica

AT-LEAST-ONCE (commit após processar):
  1. Consumer lê msg2
  2. Consumer processa msg2 ✓
  3. Consumer commita offset=3
  Se o consumer cair entre 2 e 3: msg2 será reprocessada na reconexão
  Resultado: pode duplicar, nunca perde
```

**Na prática, use at-least-once + Inbox Pattern** (deduplicação no consumidor).
É o equilíbrio correto entre confiabilidade e complexidade.

### Exactly-once no Kafka

Kafka oferece **Exactly-Once Semantics (EOS)** desde a versão 0.11, em dois níveis:

**Idempotent producer** (configuração `enable.idempotence=true`):
O broker rejeita mensagens duplicadas do mesmo producer em caso de retry, garantindo
que a mesma mensagem não seja escrita duas vezes no log.

**Transactional API** (producer + consumer em transação):
Permite que uma operação de "lê, processa, escreve em outro topic" seja atômica. Se
a transação falha, nenhuma mensagem é produzida no topic destino.

Importante: a semântica exactly-once vale **dentro do ecossistema Kafka** (topic-to-topic).
Se o processamento envolver um banco de dados externo, a garantia exactly-once depende de
coordenar o commit Kafka com o commit do banco — o que requer o Outbox Pattern ou
sagas transacionais. Não existe exactly-once "de graça" em sistemas distribuídos.

### Auto-commit vs Manual commit

```csharp
// AUTO-COMMIT: Kafka commita automaticamente a cada intervalo configurado
// Risco: commit pode ocorrer antes do processamento → at-most-once
// Aceitável apenas se perda de mensagem for tolerável (logs, métricas)

// MANUAL COMMIT (recomendado):
var resultado = await consumer.ConsumeAsync(cancellationToken);
try
{
    await ProcessarMensagem(resultado.Message);
    consumer.Commit(resultado);   // commit APÓS processamento — at-least-once
}
catch (Exception ex)
{
    // Não faz commit — mensagem será reprocessada
    logger.LogError(ex, "Falha ao processar {Offset}", resultado.Offset);
}
```

---

## Consumer Lag — observabilidade

**Consumer lag** é a diferença entre o offset mais recente produzido e o offset atual
do consumer group. É a métrica mais importante para monitorar a saúde de um consumer.

```
Producer offset:  1500  (última mensagem escrita na partition)
Consumer offset:   980  (última mensagem processada pelo consumer)
Lag:               520  (mensagens pendentes de processamento)

Lag crescente → consumer não acompanha a taxa de produção
  → Ação: aumentar instâncias do consumer
  → Ou: otimizar o processamento por mensagem
```

Ferramentas como **Confluent Control Center**, **Kafka UI**, **Datadog** e **Prometheus
+ Grafana** expõem o consumer lag em tempo real. Um alerta de lag crescente é o sinal
mais precoce de problemas de capacidade.

---

## Schema Evolution com Kafka

Em Kafka, o schema dos eventos precisa evoluir de forma compatível — consumers que
lêem mensagens antigas no log precisam continuar funcionando.

### Sem Schema Registry

Adotar convenções manuais (campos opcionais, não renomear, não remover) como descrito
em [Event-Driven Architecture — Schema Evolution](./01-event-driven-vs-sourcing.md#schema-evolution).

### Com Schema Registry

Para sistemas maiores, o **Schema Registry** (parte do ecossistema Confluent) centraliza
e valida schemas. O producer registra o schema antes de publicar. O consumer busca o
schema para desserializar. Se uma mudança quebra a compatibilidade, o registry rejeita
o schema na publicação — antes que o problema chegue ao topic.

Formatos suportados: **Avro** (mais comum), **Protobuf**, **JSON Schema**.

---

## Retention e Replay

Kafka retém mensagens por um período configurável (ou por tamanho):

```
log.retention.hours=168    # 7 dias (padrão)
log.retention.bytes=10GB   # ou por tamanho
```

Isso habilita **replay**: qualquer consumer group pode resetar seu offset para um
ponto anterior e reprocessar mensagens históricas.

Casos de uso de replay:
- Nova feature precisa processar os últimos 30 dias de pagamentos
- Bug corrigido: reprocessar mensagens que foram processadas incorretamente
- Novo serviço que entrou no ar depois que os eventos já foram produzidos

```csharp
// Resetar offset para o início do topic (reprocessar tudo)
consumer.Assign(new TopicPartitionOffset("pagamentos", partition: 0, offset: Offset.Beginning));

// Resetar para uma data específica
var timestamp = new DateTime(2026, 1, 1);
var offsets = await consumer.OffsetsForTimes(
    new[] { new TopicPartitionTimestamp("pagamentos", 0, new Timestamp(timestamp)) });
consumer.Assign(offsets);
```

**Log Compaction**: alternativa ao retention por tempo. O Kafka mantém apenas o último
evento por key. Útil quando o importante é o estado mais recente (não o histórico completo).
Exemplo: cache de configurações onde só interessa o valor atual por chave.

---

## Quando usar Kafka

- **Event streaming de alto volume**: aplicações com milhões de mensagens por segundo
- **Múltiplos consumer groups independentes**: auditoria, notificação e extrato lendo os mesmos eventos de formas independentes
- **Replay histórico**: "reprocesse os últimos 30 dias com a nova lógica"
- **Dados de referência**: sistemas que precisam manter estado derivado de um stream contínuo
- **CDC (Change Data Capture)**: captura de mudanças de banco de dados em tempo real via Debezium + Kafka

---

## Quando NÃO usar Kafka

- **Roteamento flexível por conteúdo**: Kafka roteia por partition key e topic. RabbitMQ tem exchanges com routing complexo.
- **Mensagens com TTL muito curto**: Kafka é projetado para retenção. Se a mensagem expira em segundos, é overhead desnecessário.
- **Equipes pequenas sem experiência**: operar um cluster Kafka (KRaft, replication, partitioning, schema registry) exige maturidade operacional.
- **Latência sub-milissegundo**: o overhead de coordenação do Kafka o coloca na faixa de milissegundos. Redis Pub/Sub ou gRPC para latências críticas.
- **Request-Reply (RPC)**: Kafka não é projetado para esse padrão. RabbitMQ suporta nativamente.

---

## Resumo

- Kafka é um **log distribuído**, não uma fila — mensagens persistem após o consumo
- Um topic é dividido em **partitions** — a unidade de paralelismo e ordering
- A **partition key** determina em qual partition a mensagem vai; mesma key = mesma partition = ordem garantida
- **Consumer groups** isolam o consumo: groups diferentes lêem o mesmo log independentemente
- O paralelismo máximo por group é `min(consumers, partitions)`
- **At-least-once** é a semântica prática; exactly-once existe mas só dentro do ecossistema Kafka
- **Consumer lag** é a métrica central de observabilidade
- **Replay** é natural — reset de offset permite reprocessar histórico

---

## Perguntas para fixação

1. Qual a diferença fundamental entre Kafka e uma fila de mensagens tradicional?
2. Por que usar o ID do aggregate como partition key?
3. O que acontece se você tiver mais consumers do que partitions no mesmo group?
4. Qual a diferença entre auto-commit e manual commit? Qual escolher?
5. O que é consumer lag e o que um lag crescente indica?
6. O que é replay e quando você o usaria?
7. Kafka garante exactly-once sempre? Explique.
8. O que é uma "hot partition" e como evitar?

---

## Perguntas de entrevista

**Iniciante**

> Kafka é uma fila?

Não exatamente. Kafka é um log distribuído — a diferença principal é que mensagens
consumidas permanecem no log. Isso habilita replay e múltiplos consumer groups
independentes lendo as mesmas mensagens. Dentro de um consumer group, se comporta
como fila com competing consumers; entre groups, se comporta como Pub/Sub fan-out.

> O que é offset?

Offset é o número sequencial que identifica uma mensagem dentro de uma partition.
O consumer salva (commita) o offset da última mensagem processada para saber onde
continuar depois de uma pausa ou reinicialização.

**Intermediário**

> O que acontece com a ordenação quando você não usa partition key?

Sem partition key, as mensagens são distribuídas em round-robin entre partitions.
Dentro de cada partition, a ordem é garantida. Mas mensagens da mesma entidade podem
ir para partitions diferentes, chegando ao consumer em ordem arbitrária.
Com partition key (ex.: ID da cobrança), todos os eventos da mesma cobrança sempre
vão para a mesma partition, garantindo ordem.

> Qual a diferença entre at-least-once e exactly-once no Kafka?

At-least-once: commit após processamento — pode duplicar se o consumer cair antes
do commit, mas nunca perde. Exactly-once via EOS: o Kafka garante que a mensagem
não será duplicada no log (idempotent producer) e que operações topic-to-topic são
atômicas (transactional API). Para sistemas que interagem com bancos externos, a
garantia exactly-once ainda requer coordenação adicional (Outbox Pattern).

**Avançado**

> Como você implementaria replay seletivo em produção?

Resetar o offset para uma timestamp específica usando `offsetsForTimes()`. Criar um
consumer group temporário (ou uma nova instância com um group ID diferente do grupo
de produção) para não afetar o consumo atual. Processar os eventos com lógica idempotente
(Inbox Pattern) para evitar side-effects duplicados em caso de reprocessamento parcial.
Monitorar o lag desse group temporário até alcançar o head do log.

> Como você escolhe o número de partitions?

O número de partitions limita o paralelismo máximo do consumer group. Regra prática:
defina o paralelismo máximo esperado para o consumer mais intensivo. Aumentar partitions
depois é possível mas muda o mapeamento de hash key → partition, potencialmente reordenando
eventos do mesmo aggregate. Por isso, é melhor começar com um número maior do que o
necessário imediato (ex.: 12 ou 24) do que precisar aumentar depois.

---

## Relação com outros documentos

- [Pub/Sub](./03-pub-sub.md) — Kafka implementa o padrão Pub/Sub com log distribuído
- [RabbitMQ](./05-rabbitmq.md) — comparativo completo: modelo, casos de uso, trade-offs
- [Event Sourcing](./02-event-sourcing.md) — Kafka como event store em larga escala (log compaction)
- [Padrões de Confiabilidade](./06-reliability-patterns.md) — Outbox Pattern para publicação atômica; Inbox Pattern para idempotência
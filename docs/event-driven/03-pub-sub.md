# Pub/Sub — Publish/Subscribe

> **Pré-requisito**: [Event-Driven Architecture — Fundamentos](./01-event-driven-vs-sourcing.md)  
> **Próximo documento**: [Apache Kafka](./04-kafka.md)

---

## O que é Pub/Sub?

**Pub/Sub é um padrão de comunicação** — não uma tecnologia.

O padrão define que produtores (**publishers**) publicam mensagens em um canal intermediário
(**topic**, **exchange**, **bus**) sem saber quem irá recebê-las. Consumidores (**subscribers**)
se registram para receber mensagens desse canal sem saber quem as produziu.

Kafka, RabbitMQ, Google Pub/Sub, Azure Service Bus e SNS são **tecnologias** que implementam
o padrão Pub/Sub — cada uma com suas características e trade-offs.

```
Sem Pub/Sub (acoplamento direto):
  Publisher ──► Subscriber1   (referência direta)
  Publisher ──► Subscriber2   (referência direta)
  Publisher ──► Subscriber3   (referência direta)
  Problema: Publisher precisa conhecer e chamar cada Subscriber.

Com Pub/Sub (desacoplado via canal):
  Publisher ──► [Canal] ──► Subscriber1
                        ──► Subscriber2
                        ──► Subscriber3
  Benefício: Publisher só conhece o canal. Subscribers adicionados sem modificar o Publisher.
```

---

## Pub/Sub vs Observer (GoF)

A semelhança com o padrão Observer do GoF causa confusão. A diferença é fundamental:

| Aspecto | Observer (GoF) | Pub/Sub |
|---|---|---|
| **Acoplamento** | Subject conhece a lista de Observers | Publisher não conhece Subscribers |
| **Intermediário** | Nenhum — Subject notifica diretamente | Canal independente (broker, bus) |
| **Escopo** | In-process, mesma aplicação | Cross-process, cross-network |
| **Runtime** | Tipicamente síncrono | Tipicamente assíncrono |
| **Adição de subscriber** | Requer referência ao Subject | Subscriber registra-se no canal |

Observer é útil dentro de uma aplicação. Pub/Sub é útil entre sistemas.

---

## As três variações do padrão

### 1. Fan-out (Broadcast)

Todos os subscribers recebem a mensagem. O canal não filtra nada.

```
Publisher ──► Canal ──► Subscriber A  (recebe tudo)
                    ──► Subscriber B  (recebe tudo)
                    ──► Subscriber C  (recebe tudo)
```

**Implementações**: RabbitMQ Fanout Exchange, Kafka (todos os consumer groups lêem o mesmo topic).  
**Caso de uso**: "Nova configuração de tarifas" — todos os serviços precisam receber.

### 2. Topic-based

Subscribers se registram para tópicos específicos. O canal roteia por nome.

```
Publisher A ──► topic "pagamentos.pix"   ──► Subscriber PIX    (só processa PIX)
Publisher B ──► topic "pagamentos.ted"   ──► Subscriber TED    (só processa TED)
Publisher C ──► topic "fraudes.alerta"   ──► Subscriber Fraude (só processa fraudes)
```

**Implementações**: Kafka (topics), RabbitMQ Topic Exchange.  
**Caso de uso**: diferentes tipos de evento roteados para diferentes equipes de processamento.

### 3. Content-based

O canal filtra baseado no **conteúdo da mensagem** (payload), não apenas no nome.

```
Mensagem: { tipo: "pagamento", valor: 5000.00, banco: "itau", risco: "alto" }
    │
    ├── Filtro: valor > 1000 AND risco == "alto"  ──► Subscriber AntiFraude
    ├── Filtro: banco == "itau"                   ──► Subscriber MonitorItau
    └── Filtro: tipo == "pagamento"               ──► Subscriber Auditoria (recebe todos)
```

**Implementações**: RabbitMQ Headers Exchange (filtra por headers da mensagem).  
**Atenção**: o RabbitMQ Topic Exchange filtra por **routing key** (metadado da mensagem),
não pelo conteúdo do payload. São mecanismos diferentes.

---

## Problemas que Pub/Sub resolve

### Múltiplos consumidores para o mesmo evento

```
PagamentoConfirmadoEvento publicado UMA vez:
  │
  ├──► NotificacaoService   → envia SMS ao cliente
  ├──► ExtratoService       → registra o débito no extrato
  ├──► PontosService        → credita pontos de fidelidade
  └──► AuditoriaService     → grava log para compliance

Novo requisito: adicionar AntiFraudeService
  → Apenas registra novo subscriber no canal
  → Nenhum dos outros serviços é modificado
  → PagamentoService não precisa saber que AntiFraudeService existe
```

### Desacoplamento temporal

```
10:30 → PagamentoConfirmadoEvento publicado (ServicoPagemento funcionou normalmente)
10:30 → NotificacaoService está em manutenção planejada
11:00 → NotificacaoService volta ao ar
11:01 → NotificacaoService processa o evento do backlog → SMS enviado

REST alternativo: POST /notificacoes às 10:30 → 503 → mensagem perdida
```

### Escalabilidade independente

```
Topic "pagamentos" com 6 partitions:

  Grupo "notificacoes" (2 instâncias — processa rapidamente):
    Instância A: partitions 0, 1, 2
    Instância B: partitions 3, 4, 5

  Grupo "antifraude" (6 instâncias — processamento intensivo de ML):
    Instância 1: partition 0
    Instância 2: partition 1
    ... (1 por partition — paralelo máximo)

Cada consumer group escala de forma totalmente independente.
```

---

## Competing Consumers

Dentro de um consumer group, múltiplas instâncias processam do **mesmo** topic/fila,
mas cada mensagem é processada por **apenas uma** delas. É o padrão de load balancing.

```
Fila "processamento-pagamentos":
  Mensagem A ──► Instância 1 (processa)
  Mensagem B ──► Instância 2 (processa)
  Mensagem C ──► Instância 3 (processa)

  → 3 mensagens processadas em paralelo
  → Se Instância 2 cai, broker redistribui para Instância 1 ou 3
  → Escala horizontal adicionando instâncias
```

Diferença de Pub/Sub (fan-out) vs Competing Consumers:

| Modelo | Comportamento | Caso de uso |
|---|---|---|
| **Pub/Sub fan-out** | Todos os subscribers recebem a mensagem | Múltiplos sistemas reagem ao mesmo evento |
| **Competing Consumers** | Apenas um dos workers recebe a mensagem | Distribuição de carga entre instâncias do mesmo serviço |

Kafka e RabbitMQ implementam os dois:
- Kafka: consumer groups diferentes = fan-out; instâncias no mesmo group = competing consumers
- RabbitMQ: fanout exchange = fan-out; múltiplos consumers na mesma queue = competing consumers

---

## Backpressure — quando o consumidor não acompanha

Backpressure é a pressão que o consumidor aplica de volta ao sistema quando não consegue
processar mensagens na velocidade que chegam.

```
Produtor: 10.000 mensagens/segundo
Consumidor: consegue processar 1.000 mensagens/segundo

Sem backpressure:
  → Fila cresce indefinidamente → memória esgotada → sistema cai

Com backpressure (dois mecanismos):

  1. Prefetch limit (RabbitMQ):
     Consumer declara que só quer receber N mensagens por vez.
     Broker só envia mais quando o consumer deu ACK nas anteriores.

  2. Consumer lag + auto-scaling (Kafka):
     Monitorar o lag (diferença entre offset do produtor e offset do consumer).
     Se o lag cresce, adicionar mais instâncias do consumer.
     Kafka Streams e KEDA (Kubernetes) fazem isso automaticamente.
```

Se o broker não suportar backpressure nativo, o sistema precisa de um mecanismo externo
(circuit breaker, throttling) para evitar que o consumidor seja sobrecarregado.

---

## Quando NÃO usar Pub/Sub

### Resposta síncrona obrigatória

```
Usuário: "Qual o saldo da minha conta?"
  → Precisa de resposta imediata
  → Pub/Sub adiciona latência sem benefício
  → REST/gRPC é a escolha correta
```

### Um único consumidor previsível

Se um evento só tem um consumidor e nunca terá mais, a complexidade do broker
pode não compensar. Uma chamada REST direta é mais simples de desenvolver, operar e debugar.

### Debugging e rastreabilidade são prioridade máxima

Rastrear o fluxo de um evento assíncrono entre múltiplos serviços requer correlation IDs,
distributed tracing e ferramentas de observabilidade. Para equipes sem essa infraestrutura,
o debugging pode ser muito mais custoso do que em sistemas síncronos.

### Garantia de ordem global é essencial

Pub/Sub garante ordem dentro de uma partition/fila — não globalmente entre partitions.
Se o domínio exige que eventos de entidades diferentes sejam processados em ordem absoluta,
a arquitetura precisa de cuidados especiais que aumentam a complexidade.

---

## Comparativo: Pub/Sub vs Fila Point-to-Point

| Aspecto | Pub/Sub | Point-to-Point (Fila) |
|---|---|---|
| **Consumidores** | Múltiplos (fan-out por default) | Um único consumidor |
| **Mensagem processada por** | Cada subscriber recebe uma cópia | Apenas um consumer |
| **Desacoplamento** | Alto — publisher não conhece subscribers | Médio — há um destinatário |
| **Caso de uso** | Notificações, eventos de domínio | Filas de tarefas, job queues |

---

## Resumo

- **Pub/Sub é um padrão**, não uma tecnologia. Kafka e RabbitMQ são tecnologias que implementam o padrão.
- O **canal intermediário** (topic, exchange, bus) é o que desacopla publisher de subscriber
- **Fan-out**: todos os subscribers recebem. **Competing consumers**: apenas um recebe.
- **Backpressure** previne que consumidores lentos sejam sobrecarregados pelo broker
- Pub/Sub **não é a resposta certa** para respostas síncronas, um único consumidor fixo ou quando debugging é prioridade
- Os três padrões — Event Notification, ECST e Event Collaboration — usam Pub/Sub como mecanismo de transporte

---

## Perguntas para fixação

1. Pub/Sub é uma tecnologia ou um padrão? Qual a diferença?
2. Qual a diferença entre Pub/Sub e o padrão Observer do GoF?
3. Como Kafka e RabbitMQ implementam fan-out?
4. O que são Competing Consumers? Dê um exemplo concreto.
5. O que é backpressure e como o RabbitMQ lida com isso?
6. Quando faz sentido usar uma fila point-to-point em vez de Pub/Sub?

---

## Perguntas de entrevista

**Iniciante**

> O que é Pub/Sub?

Pub/Sub é um padrão de comunicação onde publishers publicam mensagens em um canal sem
saber quem vai receber, e subscribers se inscrevem no canal sem saber quem publicou.
O canal (broker) desacopla os dois lados. Kafka, RabbitMQ e Azure Service Bus são exemplos
de tecnologias que implementam o padrão.

**Intermediário**

> Qual a diferença entre fan-out e competing consumers?

Fan-out: todos os subscribers independentes recebem uma cópia da mensagem. Usado quando
múltiplos sistemas precisam reagir ao mesmo evento (Notificação, Extrato, Antifraude).
Competing consumers: instâncias do mesmo serviço competem pela mensagem — apenas uma
processa. Usado para distribuição de carga entre instâncias do mesmo worker.

> O que é backpressure e por que importa?

Backpressure é o mecanismo pelo qual consumidores sinalizam que não conseguem processar
na velocidade de produção. Sem backpressure, a fila cresce indefinidamente até memória
esgotar. RabbitMQ resolve com prefetch count (consumer declara quantas mensagens aguenta
ao mesmo tempo). Kafka resolve via consumer lag monitoring + auto-scaling.

**Avançado**

> Qual a diferença entre topic-based routing e content-based routing?

Topic-based usa o nome do canal/routing-key para rotear. O broker decide por metadado
da mensagem, não pelo conteúdo do payload. É eficiente e simples. Content-based inspeciona
o conteúdo do payload para decidir o roteamento — mais flexível, mais custoso, pode criar
acoplamento entre broker e schema de payload. No RabbitMQ: Topic Exchange usa routing key
(topic-based), Headers Exchange usa headers HTTP-like da mensagem (mais próximo de
content-based sem inspecionar o body).

---

## Relação com outros documentos

- [Event-Driven Architecture](./01-event-driven-vs-sourcing.md) — EDA usa Pub/Sub como mecanismo de transporte
- [Kafka](./04-kafka.md) — implementação de Pub/Sub com log distribuído
- [RabbitMQ](./05-rabbitmq.md) — implementação de Pub/Sub com exchanges e bindings
- [Padrões de Confiabilidade](./06-reliability-patterns.md) — como garantir entrega confiável em sistemas Pub/Sub
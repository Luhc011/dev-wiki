# Event-Driven Architecture — Fundamentos

> **Pré-requisito**: familiaridade com REST e com o conceito de microsserviços.  
> **Próximo documento**: [Event Sourcing](./02-event-sourcing.md)

---

## O que é Event-Driven Architecture?

Event-Driven Architecture (EDA) é um **estilo arquitetural** em que sistemas se comunicam
produzindo e consumindo **eventos** — registros de fatos que aconteceram — em vez de chamar
uns aos outros diretamente.

O ponto central é o **desacoplamento**: quem produziu o evento não sabe quem vai consumi-lo,
nem quando, nem quantos consumidores existem.

```
Sistema sem EDA (acoplamento direto):

  ServicoPagemento.ConfirmarPagamento()
      │──► NotificacaoService.EnviarSms()       ← chamada direta HTTP/gRPC
      │──► ExtratoService.RegistrarDebito()      ← chamada direta HTTP/gRPC
      └──► PontosService.CreditarPontos()        ← chamada direta HTTP/gRPC

  Problema: cada novo consumidor exige modificar ServicoPagemento.
  Se NotificacaoService estiver fora do ar, o pagamento falha.


Sistema com EDA (desacoplamento via evento):

  ServicoPagemento.ConfirmarPagamento()
      └──► publica PagamentoConfirmadoEvento no broker

  NotificacaoService    ──► consome PagamentoConfirmadoEvento
  ExtratoService        ──► consome PagamentoConfirmadoEvento
  PontosService         ──► consome PagamentoConfirmadoEvento

  Benefício: adicionar AntiFraudeService não modifica ServicoPagemento.
  Se NotificacaoService estiver fora do ar, o pagamento ocorre normalmente —
  o evento fica no broker até ele voltar.
```

---

## Vocabulário fundamental — Evento, Comando e Mensagem

Antes de avançar, é essencial distinguir três termos que aparecem juntos mas significam
coisas diferentes. Confundir os três é um erro comum e causa problemas de design.

### Mensagem — o conceito genérico

**Mensagem** é o termo genérico para qualquer dado trafegado entre sistemas via broker.
Eventos e Comandos são tipos específicos de mensagem.

```
Mensagem
├── Evento     (descreve algo que aconteceu)
└── Comando    (solicita que algo aconteça)
```

### Evento — um fato imutável no passado

Um **evento** descreve algo que **já aconteceu**. É um fato, não uma intenção.

Características:
- **Nome no passado**: `PagamentoConfirmado`, não `ConfirmarPagamento`
- **Imutável**: não pode ser desfeito — apenas um novo evento corrige o anterior
- **Produtor agnóstico**: quem publicou o evento não sabe quem vai reagir a ele
- **Broadcast por natureza**: qualquer número de consumidores pode reagir

```csharp
// Evento: algo aconteceu
public sealed record PagamentoConfirmadoEvento
{
    public Guid PagamentoId { get; init; }
    public string CpfPagador { get; init; }
    public decimal Valor { get; init; }
    public DateTime ConfirmadoEm { get; init; }
}
```

### Comando — uma intenção direcionada

Um **comando** representa uma **solicitação** para que algo aconteça. Tem um destinatário
específico e pode ser aceito ou rejeitado.

Características:
- **Nome no imperativo**: `ProcessarPagamento`, `CancelarCobranca`
- **Tem um destinatário**: enviado a um serviço específico
- **Pode falhar**: o receptor valida e pode rejeitar
- **Geralmente um consumidor**: point-to-point, não broadcast

```csharp
// Comando: solicitação direcionada
public sealed record ProcessarPagamentoCommand
{
    public Guid CobrancaId { get; init; }
    public decimal Valor { get; init; }
    public string MetodoPagamento { get; init; }  // "pix" | "ted" | "boleto"
}
```

### Comparação — Evento vs Comando

| Aspecto | Evento | Comando |
|---|---|---|
| **Representa** | Algo que aconteceu | Solicitação de que algo aconteça |
| **Nomenclatura** | Passado: `PagamentoConfirmado` | Imperativo: `ConfirmarPagamento` |
| **Destinatário** | Desconhecido / múltiplos | Específico |
| **Resposta esperada** | Nenhuma | Aceito ou rejeitado |
| **Pode falhar?** | Não (já ocorreu) | Sim |
| **Consumidores** | N (broadcast) | 1 (point-to-point) |

### Quando usar cada um na prática

```
Pagamento processado com sucesso?
    → Publicar PagamentoConfirmadoEvento
       (NotificaçãoService, ExtratoService, PontosService reagem independentemente)

Usuário quer cancelar uma cobrança?
    → Enviar CancelarCobrancaCommand para CobrancaService
       (um destinatário responsável pela decisão de aceitar ou não)
```

O erro frequente é tratar eventos como comandos disfarçados:
```
✗ Errado: publicar "EnviarSmsEvento" — isso é um comando disfarcado de evento
✓ Correto: publicar "PagamentoConfirmadoEvento" e deixar o SMS ser uma reação
```

---

## Os três padrões estruturais de EDA

EDA não é uma coisa só. Há três padrões com motivações e trade-offs diferentes.
Compreender os três evita aplicar EDA de forma genérica e errada.

### Padrão 1 — Event Notification

O sistema produtor avisa que algo aconteceu, mas **não carrega os dados no evento**.
O consumidor vai buscar o dado se precisar.

```
PagamentoCriadoEvento { PagamentoId: "abc-123" }
                                │
                                ▼
   NotificacaoService recebe o evento
   → GET /pagamentos/abc-123           ← chama de volta para buscar detalhes
   → Constrói e envia o SMS
```

**Quando usar**: quando o dado muda rapidamente e o consumidor não precisa do snapshot
do momento exato do evento, ou quando o payload seria muito grande.

**Trade-off**: ainda há acoplamento temporal para a leitura — o consumidor precisa chamar
o produtor de volta. Se o produtor estiver fora do ar, o processamento falha mesmo com o
evento no broker.

### Padrão 2 — Event-Carried State Transfer

O evento carrega **todos os dados necessários**. O consumidor não precisa chamar ninguém.

```
PagamentoConfirmadoEvento {
    PagamentoId: "abc-123",
    CpfPagador: "529.982.247-25",
    Valor: 500.00,
    DataConfirmacao: "2026-01-15T10:30:00Z",
    NomePagador: "João Silva",
    ChavePix: "529.982.247-25@cpf"
}
                    │
                    ▼
   NotificacaoService recebe tudo o que precisa — sem chamada de volta
   ExtratoService recebe tudo o que precisa — sem chamada de volta
   PontosService recebe tudo o que precisa — sem chamada de volta
```

**Quando usar**: quando o desacoplamento total é o objetivo principal. É o padrão mais
robusto para sistemas distribuídos porque elimina o acoplamento temporal da leitura.

**Trade-off**: eventos ficam maiores. Se o schema do dado mudar, todos os consumidores
precisam lidar com a evolução do contrato (ver [Schema Evolution](#schema-evolution)).

### Padrão 3 — Event Collaboration (Coreografia)

Múltiplos serviços colaboram para completar um processo, cada um reagindo ao evento
do anterior e publicando seu próprio evento.

```
Fluxo de pagamento por coreografia:

  [Usuario] ──► [Pagamento]               
                    │ publica: PagamentoIniciadoEvento
                    │
                    ▼
             [AntiFraude] consome PagamentoIniciadoEvento
                    │ publica: AnaliseConcluida(aprovado=true)
                    │
                    ▼
             [Processador] consome AnaliseConcluida
                    │ publica: PagamentoConfirmadoEvento
                    │
                    ▼
             [Notificacao] consome PagamentoConfirmadoEvento
                    │ publica: SmsEnviadoEvento
```

**Quando usar**: processos de negócio onde cada etapa é independente e pode falhar
de forma isolada.

**Trade-off**: o fluxo completo é difícil de visualizar — está distribuído entre
vários serviços. Debugging é complexo. Compare com Orquestração (Saga), onde um
orquestrador central controla o fluxo.

---

## Desacoplamento espacial e temporal

EDA resolve dois tipos de acoplamento que sistemas síncronos não conseguem eliminar.

### Desacoplamento espacial

Com REST síncrono, o chamador precisa saber **onde** o receptor está (URL, IP, porta).

Com EDA, o produtor publica no broker. Não sabe se o consumidor está em São Paulo,
na AWS us-east-1, ou se são 5 instâncias em execução.

```
REST:  ServicoA ──► http://servico-b:8080/endpoint   ← acoplado ao endereço
EDA:   ServicoA ──► broker["pagamentos"]              ← desacoplado do endereço
```

### Desacoplamento temporal

Com REST síncrono, o receptor precisa estar **online** no momento da chamada.

Com EDA, o broker retém a mensagem. O consumidor pode processar horas depois.

```
Cenário real: manutenção planejada do serviço de notificações

REST:
  10:30 → POST /notificacoes → 503 Service Unavailable → mensagem perdida

EDA:
  10:30 → PagamentoConfirmadoEvento publicado no broker (sucesso)
  11:00 → NotificacaoService volta do ar, processa o backlog
  11:01 → SMS enviado ao cliente — com 31 minutos de atraso, mas sem perda
```

---

## Event-Driven Architecture vs Event Sourcing

Este é o ponto de confusão mais frequente. São conceitos **ortogonais** — independentes.

| | Event-Driven Architecture | Event Sourcing |
|---|---|---|
| **Responde a** | Como sistemas se **comunicam** | Como sistemas **armazenam estado** |
| **Problema** | Desacoplamento entre serviços | Auditoria e rastreabilidade de estado |
| **Artefato** | Evento de domínio (comunicação) | Evento de aggregate (persistência) |
| **Escopo** | Entre sistemas/serviços | Dentro de um serviço/aggregate |
| **Depende de** | Broker de mensagens | Event Store |

### A combinação possível

```
                    COMBINAÇÕES POSSÍVEIS

EDA SEM Event Sourcing:
  Microsserviços publicam eventos no Kafka
  Cada serviço usa PostgreSQL convencional (estado atual)
  ✓ Desacoplamento     ✗ Auditoria via eventos

Event Sourcing SEM EDA:
  Monolito com event store interno (EventStoreDB, tabela de eventos)
  Sem publicar eventos para outros sistemas
  ✓ Auditoria completa   ✗ Integração com outros serviços

EDA + Event Sourcing:
  Serviço de Pagamentos usa event store para persistir estado
  E publica eventos de domínio no Kafka para outros serviços reagirem
  ✓ Auditoria   ✓ Desacoplamento   (maior complexidade)

Nenhum dos dois:
  CRUD com REST — simples, direto, suficiente para muitos domínios
```

### Exemplo concreto — Extrato bancário

O extrato bancário **É** Event Sourcing:
- Cada crédito e débito é um evento imutável armazenado em sequência
- O saldo é **derivado** somando todos os lançamentos (replay)
- Nunca se edita um lançamento — apenas se insere um estorno (novo evento)

Mas o sistema bancário **pode não ser EDA**: o caixa pode fazer INSERT na tabela de
lançamentos sem publicar eventos para outros sistemas. É Event Sourcing sem EDA.

---

## Quando EDA melhora o sistema

- **Múltiplos consumidores para o mesmo evento**: adicionar um novo serviço consumidor
  sem modificar o produtor
- **Desacoplamento temporal**: consumidor pode estar offline sem causar falha no produtor
- **Escalabilidade independente**: cada consumidor escala à sua própria taxa
- **Auditabilidade de fluxo**: todos os eventos ficam no broker, rastreáveis
- **Resiliência a falhas parciais**: falha num consumidor não afeta os outros

---

## Quando EDA piora o sistema

Esta seção é mais importante do que parece. EDA não é "sempre melhor".

### Domínios simples

Se um serviço precisa acionar apenas um outro serviço e a resposta é necessária
imediatamente, REST é mais simples e mais fácil de debugar.

```
✗ Complexidade desnecessária:
  Cadastro → publica UsuarioCriadoEvento → EmailService consome → envia email
  (um broker, uma fila, tratamento de falha assíncrona, tudo isso para enviar um email?)

✓ Mais simples:
  Cadastro → POST /emails/boas-vindas → retorna 200 OK
```

### Quando a ordem é crítica e difícil de garantir

EDA em sistemas distribuídos torna a ordenação complexa. Eventos de partições diferentes
podem chegar fora de ordem. Se o domínio exige ordem absoluta entre agregados diferentes,
EDA adiciona complexidade sem reduzir acoplamento.

### Quando a resposta síncrona é obrigatória

```
Usuário pergunta: "Tenho saldo para essa compra?"
  → Precisa de resposta imediata
  → REST síncrono é a resposta certa
  → Colocar isso em EDA adiciona latência e complexidade sem benefício
```

### Equipes pequenas sem maturidade operacional

Um broker em produção precisa de:
- Monitoramento de lag de consumidores
- Alertas de DLQ
- Gestão de schema evolution
- Tratamento de poison messages
- Procedimentos de disaster recovery

Para uma equipe de 3 pessoas trabalhando num MVP, esse overhead pode ser maior
do que o benefício do desacoplamento.

---

## Schema Evolution

Quando um evento é publicado por um serviço e consumido por outro, o contrato
desse evento é uma **API pública**. Quebrá-lo quebra todos os consumidores.

```
PagamentoConfirmadoEvento v1:
  { "pagamentoId": "abc", "valor": 500 }

PagamentoConfirmadoEvento v2 (BREAKING CHANGE):
  { "id": "abc", "valorEmCentavos": 50000 }   ← renomeou campos!
  Todos os consumidores que esperam "pagamentoId" e "valor" quebram.
```

### Regras de evolução segura de contratos

**O que é SEGURO fazer:**
- Adicionar campos opcionais novos (consumidores antigos ignoram)
- Adicionar novos valores de enum que consumidores não usam

**O que QUEBRA consumidores:**
- Renomear campos existentes
- Remover campos
- Mudar o tipo de um campo
- Mudar a semântica de um campo (ex.: `valor` passa a ser em centavos)

```csharp
// ✓ Evolução segura — adicionar campo opcional
public sealed record PagamentoConfirmadoEvento
{
    public Guid PagamentoId { get; init; }
    public decimal Valor { get; init; }
    public DateTime ConfirmadoEm { get; init; }
    public string? MetodoPagamento { get; init; }  // ← novo, nullable = compatível
}

// ✗ Breaking change — renomear campo existente
public sealed record PagamentoConfirmadoEvento
{
    public Guid Id { get; init; }       // ← era PagamentoId, quebra consumidores
    public decimal Valor { get; init; }
}
```

### Estratégia de versionamento explícito

Quando uma mudança é inevitável, versione o tipo do evento:

```csharp
// Consumidores antigos continuam recebendo V1 até migração
public sealed record PagamentoConfirmadoEventoV1 { ... }

// Novos consumidores recebem V2
public sealed record PagamentoConfirmadoEventoV2 { ... }

// No broker: dois topics separados, ou campo "versao" no header
```

Para sistemas de grande escala, ferramentas como **Apache Avro** com **Schema Registry**
automatizam a compatibilidade: o registry rejeita publicação de schemas incompatíveis.

---

## Resumo

- **Evento**: fato imutável que aconteceu — nome no passado, broadcast, agnóstico de consumidores
- **Comando**: intenção direcionada — nome imperativo, um destinatário, pode falhar
- **EDA resolve comunicação entre sistemas** — não resolve como persistir estado
- **Os três padrões**: Event Notification (avisa), Event-Carried State Transfer (carrega tudo), Event Collaboration (coreografia)
- **Desacoplamento espacial**: produtor não sabe onde o consumidor está
- **Desacoplamento temporal**: consumidor pode processar horas depois sem perda
- **EDA ≠ Event Sourcing**: são ortogonais — um resolve comunicação, o outro resolve persistência
- **EDA nem sempre vale a pena**: domínios simples, respostas síncronas obrigatórias e equipes pequenas são contra-indicações reais
- **Schema evolution**: contratos de eventos são APIs públicas — quebrá-los quebra consumidores

---

## Perguntas para fixação

1. Qual a diferença entre um Evento e um Comando? Dê um exemplo de cada no domínio bancário.
2. Por que `EnviarSmsEvento` é um mau design de evento?
3. Em qual situação EDA piora o sistema em vez de melhorar?
4. Explique desacoplamento temporal com um exemplo concreto.
5. Por que Event Notification pode reintroduzir acoplamento temporal?
6. Qual a diferença entre Event-Carried State Transfer e Event Notification?
7. Por que renomear um campo em um evento pode quebrar produção?

---

## Perguntas de entrevista

**Iniciante**

> O que é Event-Driven Architecture?

EDA é um estilo arquitetural onde sistemas se comunicam produzindo e consumindo eventos
registros de fatos que aconteceram — em vez de chamadas diretas. O produtor não sabe
quem vai consumir o evento. O benefício central é o desacoplamento.

> Qual a diferença entre evento e comando?

Evento descreve o passado (nome no passado, imutável, broadcast, sem destinatário específico).
Comando solicita que algo aconteça (nome imperativo, tem destinatário, pode ser rejeitado).

**Intermediário**

> EDA e Event Sourcing são a mesma coisa?

Não. EDA resolve como sistemas **se comunicam** — desacoplamento via eventos.
Event Sourcing resolve como um sistema **armazena estado** — sequência de eventos como
fonte de verdade. São ortogonais: um banco pode usar event store internamente sem publicar
eventos para outros serviços (Event Sourcing sem EDA), ou publicar eventos no Kafka com
banco relacional convencional (EDA sem Event Sourcing).

> Quando você NÃO usaria EDA?

Quando a resposta precisa ser síncrona e imediata, quando o domínio é simples com poucos
consumidores, quando a equipe não tem maturidade operacional para operar um broker, ou
quando a ordem absoluta entre eventos é crítica e difícil de garantir na arquitetura.

**Avançado**

> O que é Event-Carried State Transfer e quando ele é preferível ao Event Notification?

ECST carrega todos os dados necessários no evento, eliminando a necessidade do consumidor
chamar de volta o produtor. É preferível quando o desacoplamento total é o objetivo —
uma falha no produtor não impede o consumidor de processar o evento recebido. O trade-off
é eventos maiores e necessidade de schema evolution mais cuidadosa. Event Notification
é preferível quando o payload seria muito grande ou quando o dado muda entre o momento
do evento e o consumo, tornando o snapshot no evento desatualizado.

> Como você lidaria com breaking changes em contratos de eventos em produção?

Estratégia de versionamento explícito: introduzir V2 do evento em paralelo ao V1.
Novos produtores publicam V2. Consumidores são migrados gradualmente. V1 é descontinuado
apenas quando todos os consumidores migraram. Alternativa para sistemas de escala: usar
Schema Registry (Avro/Protobuf) que rejeita schemas incompatíveis na publicação.

---

## Relação com outros documentos

- [Event Sourcing](./02-event-sourcing.md) — como persistir estado com eventos
- [Pub/Sub](./03-pub-sub.md) — o padrão de distribuição que EDA usa
- [Kafka](./04-kafka.md) — implementação de broker para EDA de alto volume
- [RabbitMQ](./05-rabbitmq.md) — implementação de broker para EDA com roteamento flexível
- [Padrões de Confiabilidade](./06-reliability-patterns.md) — como garantir entrega e idempotência
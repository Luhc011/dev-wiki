# Microservices Patterns — Guia Completo

## 1. O que são Microservices Patterns?

Padrões de microsserviços são **soluções documentadas para problemas recorrentes**
em sistemas distribuídos — problemas que já foram encontrados e resolvidos por outras
equipes, muitas vezes da forma mais difícil possível.

Não inventamos os padrões deste módulo. Eles emergem da prática, são documentados
por autores como Chris Richardson (*Microservices Patterns*, 2018), Martin Fowler e
a equipe da Netflix, que os descobriu em produção.

### Por que padrões existem

Em um monolito:
- Uma única transação abarca tudo (ACID garantido pelo banco)
- Chamadas entre módulos são chamadas de método em memória
- Falha de um módulo = processo todo cai (mas é simples de rastrear)

Em microsserviços:
- Cada serviço tem **seu próprio banco de dados** (isolamento)
- Comunicação é via rede (pode falhar, pode atrasar, pode duplicar)
- Não existe transação distribuída simples
- Falha de um serviço pode derrubar uma cadeia inteira

Os padrões deste módulo resolvem exatamente esses problemas.

---

## 2. O problema central: sistemas distribuídos são fundamentalmente difíceis

### Consistência eventual vs. consistência forte

Em um banco relacional único:
```sql
BEGIN TRANSACTION;
  INSERT INTO cobranças ...;
  UPDATE saldo ...;
  INSERT INTO notificações ...;
COMMIT; -- tudo ou nada
```

Em microsserviços com três bancos diferentes: não existe `COMMIT` global.
Se o ServiçoA comitou mas o ServiçoB falhou, o sistema está inconsistente.

**Consistência forte** = todos os participantes veem o mesmo estado no mesmo instante.  
**Consistência eventual** = o sistema *eventualmente* convergirá para um estado consistente,
mas pode haver um período de inconsistência observável.

Microsserviços geralmente aceitam consistência eventual — e os padrões deste módulo
são ferramentas para gerenciar isso de forma controlada.

### O Teorema CAP

Eric Brewer (2000) provou que um sistema distribuído não pode garantir simultaneamente:
- **C**onsistency (consistência) — todos os nós veem os mesmos dados ao mesmo tempo
- **A**vailability (disponibilidade) — toda requisição recebe uma resposta
- **P**artition tolerance (tolerância a partição) — o sistema funciona mesmo com falhas de rede

Em sistemas reais, partições de rede são inevitáveis. Então você escolhe entre C e A.

| Escolha | Sistemas típicos | Trade-off |
|---------|-----------------|-----------|
| CP | MongoDB, HBase, ZooKeeper | Fica indisponível durante partição para manter consistência |
| AP | DynamoDB, Cassandra, CouchDB | Aceita dados desatualizados para manter disponibilidade |

### Por que two-phase commit não resolve

2PC (Two-Phase Commit) garante atomicidade distribuída, mas:
- Bloqueio: participantes ficam bloqueados aguardando o coordenador
- Ponto único de falha: se o coordenador cair entre as fases, participantes ficam bloqueados
- Não escala: latência cresce com número de participantes
- Indisponível durante partição: sacrifica A para garantir C

Os padrões deste módulo (especialmente Saga + Outbox) são alternativas práticas ao 2PC.

---

## 3. Mapa dos padrões e como se relacionam

```
 Problema: múltiplos serviços, uma operação        → SAGA
 Problema: banco E broker na mesma operação        → OUTBOX
 Problema: serviço dependente está falhando        → CIRCUIT BREAKER
 Problema: múltiplos clientes com necessidades     → BFF
           diferentes
```

### Como os padrões se complementam

```
╔══════════════════════════════════════════════════════╗
║  POST /cobranças → BFF Mobile                        ║
║                    │                                 ║
║                    ▼                                 ║
║  ServicoCobrança ──────────────────────────────────  ║
║       │          Circuit Breaker protege →           ║
║       │          ServiçoAntifraude                   ║
║       │                                              ║
║       ├── Saga coordena a transação distribuída      ║
║       │                                              ║
║       └── Outbox garante que evento é publicado      ║
║           mesmo que broker esteja temporariamente    ║
║           indisponível                               ║
╚══════════════════════════════════════════════════════╝
```

### Não existe almoço grátis

| Padrão | Problema que resolve | Complexidade introduzida |
|--------|---------------------|--------------------------|
| Saga | Transação distribuída sem 2PC | Compensação complexa, eventos de rollback, estado de saga |
| Outbox | Dual-write atômico com broker | Tabela Outbox, Worker extra, cleanup de eventos antigos |
| Circuit Breaker | Falhas em cascata | Configuração de thresholds, fallback, half-open states |
| BFF | API genérica atende mal múltiplos clientes | Mais serviços para manter, possível duplicação de lógica |

**Use quando o problema justificar a complexidade.** Um CRUD simples de 2 serviços
não precisa de Saga. Uma API servindo apenas web não precisa de BFF.

---

## 4. Pré-requisitos

Estes padrões são mais fáceis de entender com base nos módulos anteriores:

| Módulo anterior | Por que importa aqui |
|----------------|---------------------|
| **DIP (SOLID)** | Interfaces como contratos entre serviços — ex: `IServicoBacen`, `IEventBus` |
| **ISP (SOLID)** | Cada interface faz uma coisa — `IOutboxRepositorio` ≠ `IPublicadorMensagens` |
| **Clean Architecture** | Separação de camadas, Result Pattern, use cases |

---

## 5. Estrutura deste módulo

```
src/Microservices/
├── ArchitectureLab.Microservices.Shared/
│   ├── SharedKernel/   → Result<T>, Evento (base record abstrato)
│   └── Domain/         → Cobranca, CobrancaStatus, StepStatus
│
├── ArchitectureLab.Microservices.Saga/
│   ├── Events/         → Eventos de domínio (CobrancaCriada, AntifraudeAprovado...)
│   ├── Interfaces/     → IEventBus, IServicoAntifraude, IServicoDebito...
│   ├── Fakes/          → Implementações in-memory para testes
│   ├── Choreography/   → Saga Coreografia (sem orquestrador)
│   └── Orchestration/  → Saga Orquestração (SagaOrchestrator centralizado)
│
├── ArchitectureLab.Microservices.Outbox/
│   ├── Interfaces/     → IOutboxRepositorio, IInboxRepositorio, IPublicadorMensagens
│   ├── Repositories/   → Implementações in-memory
│   ├── Application/    → ServicoCobrancaComOutbox
│   └── Workers/        → OutboxWorker (BackgroundService), InboxProcessor
│
├── ArchitectureLab.Microservices.CircuitBreaker/
│   ├── Interfaces/     → IServicoBacen
│   ├── Models/         → ResultadoPagamento
│   ├── Fakes/          → ServicoBacenFake (configurável para falhas)
│   ├── Policies/       → PoliticaResiliencia (Polly v8)
│   └── Services/       → GatewayPagamentoResistente
│
└── ArchitectureLab.Microservices.Bff/
    ├── Interfaces/     → IServicoCobranca, IServicoPagamento
    ├── Models/         → CobrancaDetalhe, respostas por tipo de cliente
    ├── Fakes/          → implementações in-memory
    ├── BffMobile.cs    → resposta compacta
    ├── BffWeb.cs       → resposta completa com pagamentos
    └── BffParceiro.cs  → campos específicos para B2B
```

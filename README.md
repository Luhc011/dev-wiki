# DevWiki — Estudos de Arquitetura .NET

Repositório de teoria e prática dos princípios de arquitetura de software em C#/.NET.
Todos os exemplos usam domínio bancário (pagamentos, cobranças, notificações).

## SOLID

| Princípio | Documentação | Código | Status |
|-----------|-------------|--------|--------|
| SRP — Single Responsibility | [docs/solid/01-srp.md](docs/solid/01-srp.md) | src/Solid/DevWiki.Solid.Srp | ✅ |
| OCP — Open/Closed | [docs/solid/02-ocp.md](docs/solid/02-ocp.md) | src/Solid/DevWiki.Solid.Ocp | ✅ |
| LSP — Liskov Substitution | [docs/solid/03-lsp.md](docs/solid/03-lsp.md) | src/Solid/DevWiki.Solid.Lsp | ✅ |
| ISP — Interface Segregation | [docs/solid/04-isp.md](docs/solid/04-isp.md) | src/Solid/DevWiki.Solid.Isp | ✅ |
| DIP — Dependency Inversion | [docs/solid/05-dip.md](docs/solid/05-dip.md) | src/Solid/DevWiki.Solid.Dip | ✅ |

### Cobertura de testes — SOLID

| Módulo | Testes |
|--------|--------|
| SRP    | 16 ✅  |
| OCP    | 21 ✅  |
| LSP    | 28 ✅  |
| ISP    | 32 ✅  |
| DIP    | 14 ✅  |
| **Total SOLID** | **111** |

## Clean Architecture

| Módulo | Documentação | Código | Status |
|--------|-------------|--------|--------|
| Clean Architecture | [docs/clean-arch/README.md](docs/clean-arch/README.md) | src/CleanArch/ | ✅ |

### Documentação Clean Architecture

| Arquivo | Conteúdo |
|---------|----------|
| [docs/clean-arch/README.md](docs/clean-arch/README.md) | Visão geral, diagrama de círculos, Regra de Dependência, N-Tier vs Clean |
| [docs/clean-arch/01-domain.md](docs/clean-arch/01-domain.md) | Entities, Value Objects, Aggregate Root, Result Pattern, interfaces de domínio |
| [docs/clean-arch/02-application.md](docs/clean-arch/02-application.md) | CQRS, Commands vs Queries, ICommandHandler, IQueryHandler, orquestração |
| [docs/clean-arch/03-infrastructure.md](docs/clean-arch/03-infrastructure.md) | Ports & Adapters, repositório em memória, testabilidade |
| [docs/clean-arch/04-presentation.md](docs/clean-arch/04-presentation.md) | Minimal API, DTOs, mapeamento Result → HTTP, Program.cs |
| [docs/clean-arch/05-testes-arquitetura.md](docs/clean-arch/05-testes-arquitetura.md) | NetArchTest.Rules, pirâmide de testes, testes de domínio e aplicação |

## Microservices Patterns

| Módulo | Documentação | Código | Status |
|--------|-------------|--------|--------|
| Microservices Patterns | [docs/microservices/README.md](docs/microservices/README.md) | src/Microservices/ | ✅ |

### Documentação Microservices

| Arquivo | Conteúdo |
|---------|----------|
| [docs/microservices/README.md](docs/microservices/README.md) | CAP Theorem, mapa de padrões, trade-offs |
| [docs/microservices/01-saga.md](docs/microservices/01-saga.md) | Saga Choreography vs Orchestration, compensação, fluxos ASCII |
| [docs/microservices/02-outbox.md](docs/microservices/02-outbox.md) | Dual Write Problem, Outbox Pattern, Inbox Pattern, at-least-once |
| [docs/microservices/03-circuit-breaker.md](docs/microservices/03-circuit-breaker.md) | 3 estados (Closed/Open/Half-Open), Polly v8 ResiliencePipelineBuilder |
| [docs/microservices/04-bff.md](docs/microservices/04-bff.md) | BFF vs API Gateway, Mobile/Web/Parceiro, exemplos de código |

### Projetos Microservices

| Projeto | Papel |
|---------|-------|
| DevWiki.Microservices.Shared | SharedKernel: Result\<T\>, Evento, enums de domínio |
| DevWiki.Microservices.Saga | Saga Choreography + Orchestration com compensação |
| DevWiki.Microservices.Outbox | Outbox Pattern + Inbox Pattern + BackgroundService |
| DevWiki.Microservices.CircuitBreaker | Circuit Breaker com Polly v8 (Fallback → Retry → CB) |
| DevWiki.Microservices.Bff | BFF Mobile / Web / Parceiro |
| DevWiki.Microservices.Tests | Testes de todos os padrões |

## Event-Driven & Messaging

| Módulo | Documentação | Código | Status |
|--------|-------------|--------|--------|
| Event-Driven & Messaging | [docs/event-driven/README.md](docs/event-driven/README.md) | src/EventDriven/ | ✅ |

### Documentação Event-Driven

| Arquivo | Conteúdo |
|---------|----------|
| [docs/event-driven/README.md](docs/event-driven/README.md) | EDA vs Event Sourcing, Kafka vs RabbitMQ, mapa do módulo |
| [docs/event-driven/01-event-driven-vs-sourcing.md](docs/event-driven/01-event-driven-vs-sourcing.md) | EDA fanout, Event Sourcing vs DB tradicional, comparativo |
| [docs/event-driven/02-event-sourcing.md](docs/event-driven/02-event-sourcing.md) | 5 pilares, OCC, snapshots, projections, replay |
| [docs/event-driven/03-pub-sub.md](docs/event-driven/03-pub-sub.md) | Observer vs Pub/Sub, variações fanout/topic/content-based |
| [docs/event-driven/04-kafka.md](docs/event-driven/04-kafka.md) | Arquitetura Kafka, partitions, consumer groups, at-least-once |
| [docs/event-driven/05-rabbitmq.md](docs/event-driven/05-rabbitmq.md) | Exchange types, wildcards Topic, DLQ, ACK/NACK |

### Projetos Event-Driven

| Projeto | Papel |
|---------|-------|
| DevWiki.EventDriven.Shared | DomainEvent, AggregateEvent, Result\<T\>, StatusCobranca |
| DevWiki.EventDriven.EDA | Pub/Sub em memória: IEventBus, EventBusMemoria, handlers, extensões DI |
| DevWiki.EventDriven.EventSourcing | CobrancaAggregate, EventStore, Snapshots, Projections |
| DevWiki.EventDriven.Messaging | KafkaTopicSimulado, KafkaConsumerGroupSimulado, ExchangeRabbitMQ, FilaRabbitMQ |
| DevWiki.EventDriven.Tests | Testes de todos os padrões |

//A FAZER
## Infraestrutura & Cloud
### Documentação Infraestrutura
### Arquivos de Infraestrutura
### Projetos Infra (.NET)
### Cobertura de testes — Infraestrutura

## POO Fundamentals + GoF Design Patterns
### Documentação POO + GoF
### Projetos POO + GoF
### Cobertura de testes — POO + GoF Patterns





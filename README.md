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
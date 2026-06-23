# Clean Architecture — Guia Completo

## 1. O que é Clean Architecture?

Clean Architecture foi formalizada por Robert C. Martin (Uncle Bob) em 2012 no livro
*Clean Architecture: A Craftsman's Guide to Software Structure and Design*. Mas não
surgiu do nada — é a síntese de ideias anteriores:

| Arquitetura | Autor | Ano | Contribuição principal |
|-------------|-------|-----|----------------------|
| Hexagonal Architecture (Ports & Adapters) | Alistair Cockburn | 2005 | Separar núcleo de aplicação de atores externos |
| Onion Architecture | Jeffrey Palermo | 2008 | Camadas concêntricas, dependências apontam para dentro |
| **Clean Architecture** | Robert C. Martin | 2012 | Sistematização e nomenclatura consolidada |

### Objetivo central

Separar o que **muda rapidamente** (frameworks, bancos de dados, interfaces de usuário,
APIs externas) do que **muda devagar** (regras de negócio, casos de uso).

Por quê isso importa?

- **Frameworks mudam**: ASP.NET 5 → 6 → 7 → 8 → 10, Entity Framework 6 → Core 8
- **Bancos de dados mudam**: Oracle → PostgreSQL → NoSQL
- **UIs mudam**: MVC → Blazor → React → Mobile
- **As regras de negócio de cobranças, pagamentos e juros mudam muito menos**

Se o banco de dados dita como você estrutura seu código, uma migração de Oracle para
PostgreSQL pode exigir reescrever metade do sistema. Com Clean Architecture,
você troca o repositório em uma linha no container de DI.

---

## 2. O Diagrama de Círculos Concêntricos

```
╔══════════════════════════════════════════════════════════════╗
║  FRAMEWORKS & DRIVERS (círculo externo)                      ║
║  ┌────────────────────────────────────────────────────────┐  ║
║  │  INTERFACE ADAPTERS                                    │  ║
║  │  ┌──────────────────────────────────────────────────┐  │  ║
║  │  │  USE CASES (Application Business Rules)          │  │  ║
║  │  │  ┌────────────────────────────────────────────┐  │  │  ║
║  │  │  │  ENTITIES (Enterprise Business Rules)      │  │  │  ║
║  │  │  │                                            │  │  │  ║
║  │  │  │  Cobranca, Pagamento, Cpf, Valor           │  │  │  ║
║  │  │  │  StatusCobranca, IRepositorioCobranca      │  │  │  ║
║  │  │  └────────────────────────────────────────────┘  │  │  ║
║  │  │  ProcessarCobrancaHandler                        │  │  ║
║  │  │  ConsultarCobrancaHandler                        │  │  ║
║  │  │  RegistrarPagamentoHandler                       │  │  ║
║  │  └──────────────────────────────────────────────────┘  │  ║
║  │  Endpoints (Controllers), RepositorioCobrancaMemoria   │  ║
║  │  NotificadorConsole, DTOs                              │  ║
║  └────────────────────────────────────────────────────────┘  ║
║  ASP.NET Core, xUnit, Moq, FluentAssertions, SQL Server      ║
╚══════════════════════════════════════════════════════════════╝
```

### O que pertence a cada círculo

**Círculo 1 — Entities (Enterprise Business Rules)**
- Entidades de domínio com identidade: `Cobranca`, `Pagamento`
- Value Objects sem identidade: `Valor`, `Cpf`, `CobrancaId`
- Enums de domínio: `StatusCobranca`
- Interfaces que o domínio precisa: `IRepositorioCobranca`, `INotificadorCobranca`
- **Não pertence**: referências a Entity Framework, banco de dados, HTTP

**Círculo 2 — Use Cases (Application Business Rules)**
- Orquestração de casos de uso: `ProcessarCobrancaHandler`, `ConsultarCobrancaHandler`
- Commands e Queries: `ProcessarCobrancaCommand`, `ConsultarCobrancaQuery`
- Results: `ProcessarCobrancaResult`, `ConsultarCobrancaResult`
- **Não pertence**: classes concretas de banco, HTTP, SMTP

**Círculo 3 — Interface Adapters**
- Repositórios concretos: `RepositorioCobrancaMemoria`
- Notificadores concretos: `NotificadorConsole`
- Controllers/Endpoints: `CobrancaEndpoints`
- DTOs de API: `ProcessarCobrancaRequest`, `CobrancaResponse`
- **Não pertence**: regras de negócio

**Círculo 4 — Frameworks & Drivers**
- ASP.NET Core (minimal API)
- xUnit, FluentAssertions, Moq
- Entity Framework (quando presente)
- **Não pertence**: lógica de aplicação ou domínio

---

## 3. A Regra de Dependência — o princípio central

> "Dependências de código-fonte devem apontar apenas para dentro."
> — Robert C. Martin

```
     EXTERNO ───────────────── INTERNO
                    
  Frameworks   Adapters    Use Cases   Entities
      │             │          │          │
      ▼             ▼          ▼          │
      └─────────────┴──────────┘          │
                    └────────────────────►│
                                         
Seta: "A depende de B" (A conhece B, A referencia B)

CORRETO:
  Infrastructure ──────────────────────► Domain
  Application   ──────────────────────► Domain
  Api           ─────────────► Application
  
ERRADO:
  Domain ─────────────────────────────► Infrastructure   ← NUNCA
  Application ─────────────────────── ► Infrastructure   ← NUNCA
  Domain ──────────────────────────── ► Application      ← NUNCA
```

### Como a Regra de Dependência é DIP em escala arquitetural

No módulo DIP, vimos que:
- "Módulos de alto nível não devem depender de módulos de baixo nível"
- "Abstrações não devem depender de detalhes"

A Regra de Dependência é exatamente isso, mas aplicada à arquitetura inteira:
- Domain (alto nível) não depende de Infrastructure (baixo nível)
- Domain define `IRepositorioCobranca` (abstração)
- Infrastructure implementa `RepositorioCobrancaMemoria` (detalhe)

A diferença: DIP fala de duas classes. A Regra de Dependência fala de camadas inteiras.

### Por que violar essa regra destrói o sistema

Se Domain depende de Infrastructure:
- Testar o domínio exige banco de dados real
- Mudar o banco exige mudanças no domínio
- Não é possível reutilizar o domínio em outro projeto

Se Application depende de Infrastructure:
- Testar use cases exige banco de dados real
- A lógica de negócio fica acoplada a detalhes de implementação

---

## 4. Comparação com Arquitetura N-Tier

| Aspecto | N-Tier Tradicional | Clean Architecture |
|---------|-------------------|-------------------|
| **Estrutura** | Presentation → Business → Data | Entities ← Use Cases ← Adapters ← Frameworks |
| **Quem depende de quem** | Business depende de Data | Infrastructure depende de Domain |
| **Banco de dados** | Dita o design do sistema | Detalhe de implementação substituível |
| **Testar regras de negócio** | Precisa de banco de dados | Zero dependências externas |
| **Trocar banco de dados** | Refatoração em cascata | Trocar implementação de `IRepositorioCobranca` |
| **ORM vaza para Business** | Frequentemente sim | Nunca — domínio usa records/classes próprias |

### O problema clássico do N-Tier

```csharp
// N-TIER TRADICIONAL — Business Layer
public class CobrancaService
{
    private readonly DbContext _db;  // 💥 Business depende de Data/ORM
    
    public async Task ProcessarCobranca(CobrancaDto dto)
    {
        var entity = new CobrancaEntity // 💥 Tipo do ORM vaza para Business
        {
            CpfDevedor = dto.Cpf,
            Valor = dto.Valor
        };
        _db.Cobrancas.Add(entity);
        await _db.SaveChangesAsync();  // 💥 Detalhes de persistência no Business
    }
}
```

```csharp
// CLEAN ARCHITECTURE — Application Layer
public sealed class ProcessarCobrancaHandler(IRepositorioCobranca repositorio)
    : ICommandHandler<ProcessarCobrancaCommand, ProcessarCobrancaResult>
{
    // ✓ Depende apenas da abstração do domínio
    // ✓ Zero conhecimento de ORM, banco, ou tabelas
    // ✓ Testável com mock em < 1ms
    public async Task<Result<ProcessarCobrancaResult>> HandleAsync(...)
    {
        var cobranca = Cobranca.Criar(...);     // ✓ Domínio puro
        await repositorio.SalvarAsync(cobranca); // ✓ Abstração
    }
}
```

---

## 5. Relação com os princípios SOLID

Clean Architecture é a aplicação dos princípios SOLID em escala arquitetural:

| Princípio SOLID | Como se manifesta na Clean Architecture |
|----------------|----------------------------------------|
| **SRP** | Cada Use Case (Handler) tem uma única responsabilidade. `ProcessarCobrancaHandler` só processa, `ConsultarCobrancaHandler` só consulta. |
| **OCP** | Novo caso de uso = nova classe Handler. Os existentes não são modificados. |
| **LSP** | Qualquer repositório que implemente `IRepositorioCobranca` substitui outro. O Handler não precisa mudar. |
| **ISP** | `IRepositorioCobranca` define apenas o que o domínio precisa. Não expõe detalhes de ORM. |
| **DIP** | Domain define interfaces. Infrastructure implementa. Seta de dependência invertida. |

---

## 6. Estrutura deste projeto

```
src/CleanArch/
├── ArchitectureLab.CleanArch.Domain/
│   ├── SharedKernel/    → Result<T>, Entity (base)
│   ├── Enums/           → StatusCobranca
│   ├── ValueObjects/    → CobrancaId, Valor, Cpf
│   ├── Entities/        → Cobranca (aggregate root), Pagamento
│   └── Interfaces/      → IRepositorioCobranca, INotificadorCobranca
│
├── ArchitectureLab.CleanArch.Application/
│   ├── Abstractions/    → ICommandHandler<,>, IQueryHandler<,>
│   ├── UseCases/
│   │   ├── ProcessarCobranca/  → Command, Result, Handler
│   │   ├── ConsultarCobranca/  → Query, Result, Handler
│   │   └── RegistrarPagamento/ → Command, Result, Handler
│   └── Extensions/      → AddApplicationServices()
│
├── ArchitectureLab.CleanArch.Infrastructure/
│   ├── Repositories/    → RepositorioCobrancaMemoria
│   ├── Notifications/   → NotificadorConsole
│   └── Extensions/      → AddInfrastructureServices()
│
└── ArchitectureLab.CleanArch.Api/
    ├── Dtos/            → ProcessarCobrancaRequest, CobrancaResponse
    ├── Endpoints/       → CobrancaEndpoints
    └── Program.cs       → Minimal API, wiring
```

### Regras de referência (.csproj)

```
Domain      ──── referencia ────► (nada — zero dependências)
Application ──── referencia ────► Domain
Infrastructure ─ referencia ────► Domain + Application (para extensões)
Api         ──── referencia ────► Application + Infrastructure
```

**Domain nunca referencia nada externo** — é a essência deste padrão.
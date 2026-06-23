# Camada de Aplicação (Application Layer)

## 1. O que é a Camada de Aplicação?

A camada de Aplicação contém os **casos de uso do sistema** — as ações que um ator
externo pode realizar. Ela orquestra o domínio para executar uma intenção de negócio,
mas não contém as regras de negócio em si.

### Analogia

- **Domínio** = as regras do banco (uma cobrança vencida não pode ser paga, CPF precisa ser válido)
- **Aplicação** = o caixa do banco (recebe o pedido, chama as regras, devolve o resultado)
- **Infraestrutura** = os sistemas (banco de dados, e-mail, API externa)

O caixa não inventa as regras. Ele aplica as regras do banco para atender o cliente.

### Dependências da camada de Aplicação

```
Application → Domain   ✓ (depende apenas de Domain)
Application → Infrastructure   ✗ NUNCA (violaria a Regra de Dependência)
Application → Api              ✗ NUNCA (dependência circular)
```

```xml
<!-- ArchitectureLab.CleanArch.Application.csproj -->
<ItemGroup>
    <!-- Apenas Domain é referenciado como projeto -->
    <ProjectReference Include="..\ArchitectureLab.CleanArch.Domain\ArchitectureLab.CleanArch.Domain.csproj" />
    <!-- Abstractions de DI sem implementação (IServiceCollection) -->
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.5" />
</ItemGroup>
```

---

## 2. CQRS — Command Query Responsibility Segregation

**CQRS** (Greg Young, 2010) é o padrão de separar operações de escrita (Commands) de
operações de leitura (Queries):

### Command vs Query

| | Command | Query |
|-|---------|-------|
| **Intenção** | Mudar estado do sistema | Ler estado do sistema |
| **Retorno** | Result com dados da operação ou erro | Dados (Result<T>) |
| **Efeitos colaterais** | Sim — salva, atualiza, envia notificação | Não — apenas lê |
| **Exemplos** | `ProcessarCobrancaCommand`, `RegistrarPagamentoCommand` | `ConsultarCobrancaQuery` |
| **Idempotente?** | Não necessariamente | Sim — chamar N vezes = mesmo resultado |

### Por que separar?

```csharp
// COM CQRS — intenção explícita no tipo
await handler.HandleAsync(new ProcessarCobrancaCommand { ... }); // Cria algo
await handler.HandleAsync(new ConsultarCobrancaQuery { Id = id }); // Apenas lê

// SEM CQRS — ambiguidade
await service.ProcessarCobranca(dto);  // Cria ou consulta? Tem efeitos colaterais?
await service.GetCobranca(id);          // E se tiver side effects escondidos?
```

Benefícios:
- **Escala diferenciada**: leituras escalam horizontalmente, escritas precisam de consistência
- **Otimização separada**: queries podem ter cache; commands não devem ter
- **Rastreabilidade**: commands são eventos de mudança de estado (Event Sourcing)

### Interfaces abstratas

```csharp
// Contrato para operações de escrita
public interface ICommandHandler<TCommand, TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct = default);
}

// Contrato para operações de leitura
public interface IQueryHandler<TQuery, TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct = default);
}
```

---

## 3. Use Cases — estrutura de pasta

Cada caso de uso vive em sua própria pasta com três arquivos:

```
UseCases/
├── ProcessarCobranca/
│   ├── ProcessarCobrancaCommand.cs   ← "o que quero fazer"
│   ├── ProcessarCobrancaResult.cs    ← "o que recebo de volta"
│   └── ProcessarCobrancaHandler.cs   ← "como fazer"
├── ConsultarCobranca/
│   ├── ConsultarCobrancaQuery.cs
│   ├── ConsultarCobrancaResult.cs
│   └── ConsultarCobrancaHandler.cs
└── RegistrarPagamento/
    ├── RegistrarPagamentoCommand.cs
    ├── RegistrarPagamentoResult.cs
    └── RegistrarPagamentoHandler.cs
```

Por que não uma pasta única `Commands/` e outra `Handlers/`?

- **Coesão por feature**: tudo sobre "processar cobrança" fica junto
- **Navegabilidade**: fácil de encontrar o Handler quando você conhece o caso de uso
- **Escala**: em projetos grandes, há dezenas de casos de uso — separar por feature é essencial

---

## 4. Implementação: ProcessarCobrancaHandler

```csharp
// Command — imutável por design (record sealed)
public sealed record ProcessarCobrancaCommand
{
    public required string CpfDevedor { get; init; }
    public required decimal ValorTotal { get; init; }
    public required string Descricao { get; init; }
    public required DateOnly DataVencimento { get; init; }
}

// Handler — orquestra domínio sem conter regras de negócio
public sealed class ProcessarCobrancaHandler(
    IRepositorioCobranca repositorio,
    INotificadorCobranca notificador)
    : ICommandHandler<ProcessarCobrancaCommand, ProcessarCobrancaResult>
{
    public async Task<Result<ProcessarCobrancaResult>> HandleAsync(
        ProcessarCobrancaCommand command, CancellationToken ct = default)
    {
        // 1. Validar e criar Value Objects do domínio
        var cpfResult = Cpf.Criar(command.CpfDevedor);
        if (!cpfResult.Sucesso)
            return Result<ProcessarCobrancaResult>.Falha(cpfResult.MensagemErro!);

        var valorResult = Valor.Criar(command.ValorTotal);
        if (!valorResult.Sucesso)
            return Result<ProcessarCobrancaResult>.Falha(valorResult.MensagemErro!);

        // 2. Criar entidade via fábrica do domínio (regra de negócio está lá)
        var cobranca = Cobranca.Criar(cpfResult.Valor!, valorResult.Valor!,
                                       command.Descricao, command.DataVencimento);

        // 3. Persistir via abstração (não sabe que é memória ou Oracle)
        await repositorio.SalvarAsync(cobranca, ct);

        // 4. Notificar via abstração (não sabe que é Console ou SMTP)
        await notificador.NotificarAsync(
            cpfResult.Valor!.Formatado,
            $"Cobrança de R$ {command.ValorTotal:F2} registrada. Vencimento: {command.DataVencimento}",
            ct);

        return Result<ProcessarCobrancaResult>.Ok(
            new ProcessarCobrancaResult(cobranca.CobrancaId, cobranca.Status));
    }
}
```

### O Handler não sabe:

- Que o repositório é uma `Dictionary<>` em memória ou Oracle Database
- Que o notificador escreve no Console ou envia e-mail via SendGrid
- Que a API é Minimal API, gRPC ou GraphQL

Ele só sabe: "existe uma abstração para salvar" e "existe uma abstração para notificar".

---

## 5. O fluxo completo de um Command

```
HTTP POST /cobrancas
        │
        ▼
  CobrancaEndpoints          ← Api layer: deserializa JSON, chama handler
        │
        ▼
  ProcessarCobrancaHandler   ← Application layer: valida, orquestra
        │
        ├─── Cpf.Criar()           ← Domain: valida CPF (módulo 11)
        │
        ├─── Valor.Criar()         ← Domain: valida valor positivo
        │
        ├─── Cobranca.Criar()      ← Domain: cria entidade com ID novo
        │
        ├─── repositorio.SalvarAsync()   ← Infrastructure: persiste
        │
        └─── notificador.NotificarAsync() ← Infrastructure: notifica
        │
        ▼
  Result<ProcessarCobrancaResult>  ← volta para o endpoint → HTTP 201
```

---

## 6. Extensions — registro no container de DI

```csharp
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Registra todos os handlers como Scoped (um por request HTTP)
        services.AddScoped<
            ICommandHandler<ProcessarCobrancaCommand, ProcessarCobrancaResult>,
            ProcessarCobrancaHandler>();
        services.AddScoped<
            IQueryHandler<ConsultarCobrancaQuery, ConsultarCobrancaResult>,
            ConsultarCobrancaHandler>();
        services.AddScoped<
            ICommandHandler<RegistrarPagamentoCommand, RegistrarPagamentoResult>,
            RegistrarPagamentoHandler>();
        return services;
    }
}
```

Em `Program.cs`:
```csharp
builder.Services.AddApplicationServices();   // ← Uma linha — tudo configurado
builder.Services.AddInfrastructureServices(); // ← Repositórios e notificadores concretos
```

---

## 7. Application vs Domain Services

Uma dúvida comum: "quando uma lógica vai no domínio vs na aplicação?"

| Critério | Domínio | Aplicação |
|----------|---------|-----------|
| **"Esta regra existiria sem computador?"** | Sim → Domínio | Não → Aplicação |
| **"Um analista de negócio entende?"** | Sim → Domínio | Não → Aplicação |
| **"Requer acesso externo (banco, email)?"** | Nunca | Pode requerer |
| **Exemplos** | "CPF tem 11 dígitos e módulo 11" | "Salvar a cobrança e notificar o devedor" |

A validação de CPF (módulo 11) é uma regra de negócio — pertence ao domínio.
"Salvar no banco e enviar e-mail" é orquestração — pertence à aplicação.
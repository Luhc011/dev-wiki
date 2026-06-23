# Camada de Infraestrutura (Infrastructure Layer)

## 1. O que é a Camada de Infraestrutura?

A camada de Infraestrutura contém **implementações concretas** de interfaces definidas
no domínio. Ela sabe como persistir dados, enviar notificações, fazer chamadas HTTP —
todos os detalhes que o domínio não quer conhecer.

### Ports & Adapters (Arquitetura Hexagonal)

Alistair Cockburn chamou as interfaces de domínio de **Ports** (portas):
- **Port** = contrato/interface que o domínio define
- **Adapter** = implementação concreta na infraestrutura

```
Domain                  Infrastructure
  │                          │
  │  «Port» (interface)      │  «Adapter» (implementação)
  │  IRepositorioCobranca ◄──┤  RepositorioCobrancaMemoria
  │                          │  RepositorioCobrancaOracle    (hipotético)
  │                          │  RepositorioCobrancaPostgres  (hipotético)
  │                          │
  │  INotificadorCobranca ◄──┤  NotificadorConsole
  │                          │  NotificadorSmtp              (hipotético)
  │                          │  NotificadorSendGrid          (hipotético)
```

A regra: **domínio define a forma**, infraestrutura **preenche o detalhe**.

---

## 2. Dependências da camada de Infraestrutura

```
Infrastructure → Domain      ✓ (implementa as interfaces)
Infrastructure → Application ✓ (para extensões de DI — AddInfrastructureServices)
Infrastructure → Api         ✗ NUNCA
```

```xml
<!-- ArchitectureLab.CleanArch.Infrastructure.csproj -->
<ItemGroup>
    <ProjectReference Include="..\ArchitectureLab.CleanArch.Domain\..." />
    <ProjectReference Include="..\ArchitectureLab.CleanArch.Application\..." />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.5" />
</ItemGroup>
```

---

## 3. RepositorioCobrancaMemoria

Em produção, `IRepositorioCobranca` seria implementado com Entity Framework + SQL Server.
Para este estudo, usamos um dicionário em memória — **sem mudar uma linha do domínio ou aplicação**.

```csharp
internal sealed class RepositorioCobrancaMemoria : IRepositorioCobranca
{
    private readonly Dictionary<CobrancaId, Cobranca> _dados = [];
    
    public int CapacidadeMaxima
    {
        get => field;
        init => field = value is > 0 and <= 100_000
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "...");
    } = 10_000;

    public Task<Cobranca?> BuscarPorIdAsync(CobrancaId id, CancellationToken ct = default)
    {
        _dados.TryGetValue(id, out var cobranca);
        return Task.FromResult(cobranca);
    }
    
    public Task SalvarAsync(Cobranca cobranca, CancellationToken ct = default)
    {
        _dados[cobranca.CobrancaId] = cobranca;
        return Task.CompletedTask;
    }
    
    public Task AtualizarAsync(Cobranca cobranca, CancellationToken ct = default)
    {
        if (!_dados.ContainsKey(cobranca.CobrancaId))
            throw new KeyNotFoundException($"Cobrança {cobranca.CobrancaId.Valor} não encontrada.");
        _dados[cobranca.CobrancaId] = cobranca;
        return Task.CompletedTask;
    }
}
```

### Por que `internal`?

`RepositorioCobrancaMemoria` é `internal` porque é **um detalhe de implementação**.
Ninguém de fora do projeto Infrastructure deveria conhecer esta classe específica.
Código externo depende apenas de `IRepositorioCobranca` (domínio).

---

## 4. NotificadorConsole

```csharp
internal sealed class NotificadorConsole : INotificadorCobranca
{
    public async Task NotificarAsync(string destinatario, string mensagem, CancellationToken ct = default)
    {
        await Task.Delay(1, ct);
        var separador = """
            ══════════════════════════════════════════
            """;
        Console.WriteLine(separador);
        Console.WriteLine($"[NOTIFICAÇÃO] Para: {destinatario}");
        Console.WriteLine($"[NOTIFICAÇÃO] {mensagem}");
        Console.WriteLine(separador);
    }
}
```

Em produção, trocamos por `NotificadorSendGrid`:

```csharp
// Swap em UMA linha de configuração — domínio e aplicação não mudam
services.AddScoped<INotificadorCobranca, NotificadorSendGrid>(); // antes: NotificadorConsole
```

---

## 5. InfrastructureServiceExtensions

```csharp
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Singleton: um único repositório em memória compartilhado por todos os requests
        services.AddSingleton<IRepositorioCobranca>(
            _ => new RepositorioCobrancaMemoria { CapacidadeMaxima = 50_000 });
        
        // Scoped: nova instância por request HTTP
        services.AddScoped<INotificadorCobranca, NotificadorConsole>();
        
        return services;
    }
}
```

### Por que Singleton para o repositório em memória?

Se fosse Scoped (um por request), cada request veria um dicionário vazio.
Em memória, o estado precisa ser compartilhado. Em produção com EF Core + banco de dados,
o contexto seria Scoped (um por request, fechado ao final).

---

## 6. Testabilidade — o poder das abstrações

O maior benefício de separar domínio de infraestrutura:

```csharp
// TESTE DE USE CASE — sem banco de dados, sem console
[Fact]
public async Task ProcessarCobranca_CpfValido_DeveSalvarENotificar()
{
    // Mock de IRepositorioCobranca — substitui RepositorioCobrancaMemoria
    var repositorioMock = new Mock<IRepositorioCobranca>();
    var notificadorMock = new Mock<INotificadorCobranca>();
    
    var handler = new ProcessarCobrancaHandler(repositorioMock.Object, notificadorMock.Object);
    
    var result = await handler.HandleAsync(new ProcessarCobrancaCommand
    {
        CpfDevedor = "529.982.247-25",
        ValorTotal = 150.00m,
        Descricao = "Mensalidade",
        DataVencimento = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
    });
    
    result.Sucesso.Should().BeTrue();
    repositorioMock.Verify(r => r.SalvarAsync(It.IsAny<Cobranca>(), default), Times.Once);
    notificadorMock.Verify(n => n.NotificarAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
}
// Tempo de execução: < 5ms — sem I/O real
```

---

## 7. Trocar implementação — exemplo prático

Cenário: migrar de console para SMTP.

**Antes:**
```csharp
services.AddScoped<INotificadorCobranca, NotificadorConsole>();
```

**Depois:**
```csharp
services.AddScoped<INotificadorCobranca, NotificadorSmtp>();
// NotificadorSmtp implementa INotificadorCobranca
```

**Mudanças necessárias no domínio**: zero  
**Mudanças necessárias na aplicação**: zero  
**Mudanças necessárias nos testes de use case**: zero  

Isso é o Princípio de Substituição de Liskov (LSP) + Inversão de Dependência (DIP)
funcionando em escala arquitetural.

---

## 8. Infraestrutura ≠ Domínio — confusões comuns

| O que parece infraestrutura mas é domínio | Por quê |
|-------------------------------------------|---------|
| Validação de CPF | Regra de negócio — existiria sem banco |
| "Cobrança vencida não pode ser paga" | Regra de negócio — independe de sistema |
| Status de cobrança (Pendente, Paga, Vencida) | Conceito de domínio |

| O que parece domínio mas é infraestrutura | Por quê |
|-------------------------------------------|---------|
| `Dictionary<>` ou `DbContext` | Mecanismo de persistência |
| `Console.WriteLine` ou SendGrid | Mecanismo de notificação |
| String de conexão com banco | Configuração de infraestrutura |
# Camada de Apresentação (Presentation / Api Layer)

## 1. O que é a Camada de Apresentação?

A camada de Apresentação é a **porta de entrada** do sistema para atores externos
(usuários, outros serviços, browsers). Ela é responsável por:

1. Receber requisições externas (HTTP, gRPC, CLI, eventos)
2. Deserializar e validar o formato da entrada
3. Delegar para a camada de Aplicação
4. Serializar e retornar a resposta

### O que a Apresentação NÃO faz

- **Não contém regras de negócio** — só o domínio tem
- **Não decide "o que fazer"** — só os handlers de aplicação decidem
- **Não acessa banco de dados diretamente** — passa pelo repositório via aplicação

### Posição na Regra de Dependência

```
Api ──────► Application ──────► Domain ◄────── Infrastructure
```

A Api referencia Application (para chamar handlers) e Infrastructure (para `AddInfrastructureServices()`).
Ela **não referencia Domain diretamente** — usa DTOs para separar a API pública do modelo de domínio.

---

## 2. Por que DTOs em vez de expor Domain models?

### Sem DTOs (problemático)

```csharp
// Se expor Cobranca diretamente na API:
app.MapGet("/cobrancas/{id}", async (Guid id, IRepositorioCobranca repo) =>
{
    var cobranca = await repo.BuscarPorIdAsync(new CobrancaId(id));
    return Results.Ok(cobranca); // ← Expõe Cobranca (modelo de domínio)
});
```

Problema: qualquer mudança no modelo de domínio (renomear `CpfDevedor` para `DocumentoDevedor`)
quebra o contrato da API — todos os clientes precisam ser atualizados.

### Com DTOs (correto)

```csharp
// DTO separa contrato de API do modelo de domínio
app.MapPost("/cobrancas", async (ProcessarCobrancaRequest request, ...) =>
{
    // Converte DTO → Command
    var command = new ProcessarCobrancaCommand
    {
        CpfDevedor = request.CpfDevedor, // DTO usa nomes convenientes para clientes
        ValorTotal = request.Valor,
        ...
    };
    var result = await handler.HandleAsync(command);
    return result.Sucesso
        ? Results.Created($"/cobrancas/{result.Valor!.CobrancaId}", CobrancaResponse.De(result.Valor!))
        : Results.UnprocessableEntity(result.MensagemErro);
});
```

Agora o contrato da API é estável — pode mudar o modelo de domínio sem quebrar clientes.

---

## 3. Minimal API em .NET 10

### Por que Minimal API?

ASP.NET Core Minimal API (introduzida no .NET 6) é mais simples que Controllers para
APIs pequenas/médias:

```csharp
// Controllers (mais verboso)
[ApiController]
[Route("[controller]")]
public class CobrancasController : ControllerBase
{
    private readonly ICommandHandler<ProcessarCobrancaCommand, ProcessarCobrancaResult> _handler;
    
    public CobrancasController(ICommandHandler<...> handler) => _handler = handler;
    
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ProcessarCobrancaRequest request) { ... }
}

// Minimal API (mais simples, mesma capacidade)
app.MapPost("/cobrancas", async (
    ProcessarCobrancaRequest request,
    ICommandHandler<ProcessarCobrancaCommand, ProcessarCobrancaResult> handler) =>
{ ... });
```

### Endpoints como Extension Methods

Para manter `Program.cs` limpo, endpoints são organizados em extension methods:

```csharp
// CobrancaEndpoints.cs
public static class CobrancaEndpoints
{
    public static WebApplication MapCobrancaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cobrancas");
        group.MapPost("/", ProcessarCobranca);
        group.MapGet("/{id:guid}", ConsultarCobranca);
        group.MapPost("/{id:guid}/pagamentos", RegistrarPagamento);
        return app;
    }

    private static async Task<IResult> ProcessarCobranca(...) { ... }
    private static async Task<IResult> ConsultarCobranca(...) { ... }
    private static async Task<IResult> RegistrarPagamento(...) { ... }
}

// Program.cs — mínimo
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
var app = builder.Build();
app.MapCobrancaEndpoints();
app.Run();
```

---

## 4. Mapeamento de Result → HTTP Status

```csharp
private static async Task<IResult> ProcessarCobranca(
    ProcessarCobrancaRequest request,
    ICommandHandler<ProcessarCobrancaCommand, ProcessarCobrancaResult> handler,
    CancellationToken ct)
{
    var command = new ProcessarCobrancaCommand
    {
        CpfDevedor = request.CpfDevedor,
        ValorTotal = request.Valor,
        Descricao = request.Descricao,
        DataVencimento = request.DataVencimento
    };
    
    var result = await handler.HandleAsync(command, ct);
    
    return result.Sucesso
        ? Results.Created(
            $"/cobrancas/{result.Valor!.CobrancaId.Valor}",
            CobrancaResponse.De(result.Valor!))
        : Results.UnprocessableEntity(new { erro = result.MensagemErro });
}
```

### Tabela de mapeamentos

| Situação do Result | HTTP Status | Quando usar |
|-------------------|-------------|-------------|
| `Sucesso = true` (criação) | `201 Created` | POST que cria recurso |
| `Sucesso = true` (leitura) | `200 OK` | GET que encontrou o recurso |
| `Sucesso = false` (validação) | `422 Unprocessable Entity` | CPF inválido, valor negativo |
| Recurso não encontrado | `404 Not Found` | GET com ID inexistente |
| Servidor explodiu | `500 Internal Server Error` | Exceção não esperada |

---

## 5. DTOs — separação de responsabilidades

```csharp
// Request DTO — o que o cliente envia
public sealed record ProcessarCobrancaRequest
{
    public required string CpfDevedor { get; init; }
    public required decimal Valor { get; init; }
    public required string Descricao { get; init; }
    public required DateOnly DataVencimento { get; init; }
}

// Response DTO — o que o cliente recebe
public sealed record CobrancaResponse(
    Guid CobrancaId,
    string CpfDevedor,
    decimal ValorTotal,
    string Descricao,
    DateOnly DataVencimento,
    string Status)
{
    // Método fábrica que converte resultado de use case → DTO de API
    public static CobrancaResponse De(ConsultarCobrancaResult result) =>
        new(result.CobrancaId.Valor,
            result.CpfDevedor,
            result.ValorTotal,
            result.Descricao,
            result.DataVencimento,
            result.Status.ToString());
}
```

---

## 6. Program.cs — composição da aplicação

`Program.cs` é o único lugar onde as camadas se juntam (Composition Root):

```csharp
var builder = WebApplication.CreateBuilder(args);

// Application: registra handlers (ProcessarCobrancaHandler, etc.)
builder.Services.AddApplicationServices();

// Infrastructure: registra repositórios e notificadores concretos
builder.Services.AddInfrastructureServices();

var app = builder.Build();

app.MapCobrancaEndpoints();

app.Run();
```

O container de DI resolve as dependências em runtime:
- `ProcessarCobrancaHandler` recebe `IRepositorioCobranca` → resolve `RepositorioCobrancaMemoria`
- `ProcessarCobrancaHandler` recebe `INotificadorCobranca` → resolve `NotificadorConsole`

Nenhuma das partes internas sabe de qual implementação concreta se trata.
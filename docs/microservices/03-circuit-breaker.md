# Circuit Breaker Pattern

## 1. O problema de falhas em cascata

### Cenário: ServiçoA depende de ServiçoB

```
Cliente → ServiçoA → ServiçoB (lento/falhando)
```

O que acontece quando ServiçoB fica lento?

```
1. Requisição 1: ServiçoA chama B → aguarda (timeout 30s) → thread bloqueada
2. Requisição 2: ServiçoA chama B → aguarda (timeout 30s) → thread bloqueada
3. Requisição 3: idem
...
N. ServiçoA esgota pool de threads → começa a rejeitar novas requisições
N+1. ServiçoA cai
```

Uma falha em ServiçoB derrubou ServiçoA — que nunca teve problema próprio.
Isso é chamado de **falha em cascata** (cascade failure).

### A analogia elétrica

Em uma instalação elétrica, um curto-circuito pode sobrecarregar toda a rede.
O **disjuntor** (circuit breaker) detecta a sobrecarga e interrompe o circuito
antes que cause dano maior. Quando a causa do problema é resolvida, o disjuntor
é religado.

O Circuit Breaker em software faz o mesmo: detecta falhas repetidas e interrompe
as chamadas para o serviço com problema, evitando sobrecarregar o sistema.

---

## 2. Os três estados do Circuit Breaker

```
          FailureThreshold atingido
Closed ─────────────────────────────► Open
  ▲                                     │
  │   SuccessThreshold atingido         │ BreakDuration passou
  │                                     ▼
  └──────────────────────────────── Half-Open
```

### Closed (Fechado) — estado normal

- Todas as chamadas passam para o serviço real
- O Circuit Breaker monitora: conta sucessos e falhas
- **Transição para Open**: quando FailureRatio >= threshold em uma janela de tempo

```
[Closed] Chamada 1 → Serviço B ← resposta 200ms ✅
[Closed] Chamada 2 → Serviço B ← timeout ❌
[Closed] Chamada 3 → Serviço B ← timeout ❌
[Closed] Chamada 4 → Serviço B ← timeout ❌ ← FailureRatio = 75% > threshold
→ Transição para OPEN
```

### Open (Aberto) — modo proteção

- **Chamadas falham imediatamente** sem tentar o serviço real
- Retorna erro ou valor degradado (Fallback)
- Protege o serviço do excesso de requisições enquanto se recupera
- **Transição para Half-Open**: após BreakDuration

```
[Open] Chamada 5 → ❌ BrokenCircuitException (instantâneo, sem chamar B)
[Open] Chamada 6 → ❌ BrokenCircuitException (instantâneo)
... 30 segundos de BreakDuration ...
→ Transição para HALF-OPEN
```

### Half-Open (Semi-aberto) — período de teste

- Algumas chamadas de teste são permitidas para o serviço real
- Se essas chamadas **succedem**: transição de volta para Closed
- Se essas chamadas **falham**: transição de volta para Open

```
[Half-Open] Chamada 7 → Serviço B ← resposta 50ms ✅
[Half-Open] Chamada 8 → Serviço B ← resposta 60ms ✅
→ SuccessThreshold atingido → Transição para CLOSED
```

---

## 3. Parâmetros de configuração

| Parâmetro | Descrição | Exemplo |
|-----------|-----------|---------|
| `FailureRatio` | % de falhas para abrir o circuito | 0.5 (50%) |
| `MinimumThroughput` | Mínimo de chamadas para calcular ratio | 4 chamadas |
| `SamplingDuration` | Janela de tempo para calcular ratio | 30 segundos |
| `BreakDuration` | Tempo no estado Open antes de testar | 30 segundos |
| `SuccessThreshold` | Sucessos em Half-Open para fechar | 2 chamadas |

**Por que MinimumThroughput importa**: sem esse mínimo, 1 falha em 1 chamada = 100%
de falhas e o circuito abriria imediatamente. Precisa de volume mínimo para ser estatisticamente relevante.

---

## 4. Polly v8 — implementação em .NET

### A nova API (ResiliencePipeline)

Polly v8 (2023+) mudou completamente a API. Em vez de `Policy.Handle<>().CircuitBreaker()`,
usa-se `ResiliencePipelineBuilder`:

```csharp
var pipeline = new ResiliencePipelineBuilder<ResultadoPagamento>()
    .AddFallback(new FallbackStrategyOptions<ResultadoPagamento>
    {
        ShouldHandle = new PredicateBuilder<ResultadoPagamento>()
            .Handle<BrokenCircuitException>(),
        FallbackAction = _ =>
            ValueTask.FromResult(Outcome.FromResult(ResultadoPagamento.Degradado()))
    })
    .AddRetry(new RetryStrategyOptions<ResultadoPagamento>
    {
        MaxRetryAttempts = 2,
        ShouldHandle = new PredicateBuilder<ResultadoPagamento>()
            .Handle<HttpRequestException>()
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<ResultadoPagamento>
    {
        FailureRatio = 0.5,
        MinimumThroughput = 4,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(30),
        ShouldHandle = new PredicateBuilder<ResultadoPagamento>()
            .Handle<HttpRequestException>()
    })
    .Build();
```

### Ordem das estratégias — importa muito

Estratégias são adicionadas da **mais externa para a mais interna**:

```
Pipeline:  Fallback → Retry → CircuitBreaker → delegate real
           (1º add)  (2º add)   (3º add)
```

**Por que Retry FORA de CircuitBreaker?**

```
Com Retry fora:
  1. Delegate falha → CB conta a falha
  2. Retry tenta novamente → CB conta outra falha
  3. Depois de N falhas, CB abre → BrokenCircuitException
  4. Retry não lida com BrokenCircuitException → propaga
  5. Fallback captura BrokenCircuitException → resposta degradada

Resultado: o CB vê todas as tentativas individuais e abre corretamente.
```

```
Com Retry DENTRO de CircuitBreaker (errado para este caso):
  1. Delegate falha → Retry tenta novamente internamente
  2. CB só vê o resultado FINAL do Retry (todas as tentativas)
  3. CB teria que ver muitas falhas finais para abrir
  4. Enquanto isso, hammers (martela) o serviço com retries

Resultado: dificulta que o CB abra, continua sobrecarregando o serviço.
```

---

## 5. Fallback Strategy — o que retornar quando o circuito está aberto

### Opções de fallback

**Cache da última resposta bem-sucedida:**
```csharp
ResultadoPagamento? _ultimoSucesso;

FallbackAction = _ =>
    ValueTask.FromResult(Outcome.FromResult(
        _ultimoSucesso ?? ResultadoPagamento.Degradado()))
```

**Resposta degradada (implementado neste módulo):**
```csharp
public sealed record ResultadoPagamento(Guid CobrancaId, string CodigoAutorizacao)
{
    public bool Aprovado => CodigoAutorizacao is not (null or "DEGRADADO");
    
    // Quando o circuito está aberto, retornamos uma resposta que indica
    // que o pagamento não pode ser processado agora (mas sem lançar exceção)
    public static ResultadoPagamento Degradado() =>
        new(Guid.Empty, "DEGRADADO");
}
```

**Por que não apenas lançar exceção?**

Exceção obriga o chamador a usar try/catch. Uma resposta degradada usa o
mesmo fluxo de código — o chamador verifica `resultado.Aprovado` normalmente
e trata o caso de serviço indisponível como um caminho de negócio.

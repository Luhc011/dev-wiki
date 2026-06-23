# Camada de Domínio (Domain Layer)

## 1. O que é a Camada de Domínio?

A camada de domínio é o **coração do sistema**. Ela contém as regras de negócio
que existiriam mesmo sem computador — as mesmas regras que um especialista em cobranças
bancárias explicaria em uma reunião de negócios.

### Características obrigatórias

- **Zero dependências externas**: nenhum NuGet package de infraestrutura
- **Testável isoladamente**: os testes de domínio não precisam de mocks
- **Estável**: muda apenas quando as regras de negócio mudam — não quando o banco muda

```csharp
// Se você vê isso no Domain → VIOLAÇÃO
using Microsoft.EntityFrameworkCore;  // ❌ ORM — detalhe de infraestrutura
using System.Net.Http;                 // ❌ HTTP — detalhe de infraestrutura
using ArchitectureLab.CleanArch.Application; // ❌ Dependência circular

// O Domain só importa coisas da BCL (Base Class Library) do .NET
using System;
using System.Collections.Generic;
```

---

## 2. Entities (Entidades)

### Definição

Uma **Entity** é um objeto com **identidade própria** que persiste no tempo.
Dois objetos são a mesma entidade se têm o mesmo ID — independentemente de
seus atributos atuais.

```csharp
// Duas cobranças com os mesmos valores são cobranças DIFERENTES
// se tiverem IDs diferentes:
var cobranca1 = Cobranca.Criar(...); // Id: AAA-111
var cobranca2 = Cobranca.Criar(...); // Id: BBB-222

cobranca1 == cobranca2 // false — identidades diferentes
```

### Entity vs Value Object

| Aspecto | Entity | Value Object |
|---------|--------|-------------|
| **Identidade** | Tem ID próprio (Guid) | Não tem ID — definida pelos atributos |
| **Igualdade** | Dois objetos são iguais se têm mesmo ID | Dois objetos são iguais se têm mesmos atributos |
| **Mutabilidade** | Pode mudar de estado (Status: Pendente → Paga) | Imutável por design |
| **Exemplos** | `Cobranca`, `Pagamento` | `Valor`, `Cpf`, `CobrancaId` |

```csharp
// ENTITY — igualdade por ID
var cobrancaA = Cobranca.Criar(cpf, valor, "Mensalidade", vencimento);
var cobrancaB = cobrancaA; // mesma referência
cobrancaA == cobrancaB; // true — mesmo ID

// VALUE OBJECT — igualdade estrutural
var valor1 = Valor.Criar(100m).Valor!;
var valor2 = Valor.Criar(100m).Valor!;
valor1 == valor2; // true — mesma quantia e moeda
```

### Anemic Domain Model — o anti-pattern

**Anemic Domain Model** (Martin Fowler, 2003) é quando entidades são apenas
sacos de propriedades, sem comportamento:

```csharp
// ANEMIC — entidade sem comportamento (anti-pattern)
public class Cobranca
{
    public Guid Id { get; set; }
    public decimal Valor { get; set; }
    public string Status { get; set; } = "";
    public List<Pagamento> Pagamentos { get; set; } = [];
    // Zero comportamento aqui
}

// A lógica de negócio fica em um "serviço" externo:
public class CobrancaService
{
    public void RegistrarPagamento(Cobranca cobranca, decimal valor)
    {
        // Regra de negócio FORA da entidade — qualquer um pode chamar
        // sem respeitar as invariantes
        if (cobranca.Status == "Paga") // string hardcoded!
            throw new Exception("Já paga");
        cobranca.Status = "Paga";
        cobranca.Pagamentos.Add(new Pagamento { Valor = valor });
    }
}
```

**Rich Domain Model** (o correto):

```csharp
// RICH DOMAIN MODEL — entidade com comportamento
public sealed class Cobranca : Entity
{
    private readonly List<Pagamento> _pagamentos = [];
    
    // Regra de negócio DENTRO da entidade — invariantes sempre respeitadas
    public Result<Pagamento> RegistrarPagamento(Valor valorPago, DateTimeOffset dataHora)
    {
        if (Status is StatusCobranca.Paga)
            return Result<Pagamento>.Falha("Cobrança já está paga.");
        if (valorPago.Quantia < ValorTotal.Quantia)
            return Result<Pagamento>.Falha("Valor insuficiente.");
        
        var pagamento = Pagamento.Criar(CobrancaId, valorPago, dataHora);
        _pagamentos.Add(pagamento);
        Status = StatusCobranca.Paga; // ← apenas a própria entidade muda seu estado
        return Result<Pagamento>.Ok(pagamento);
    }
}
```

### Aggregate Root

Um **Aggregate Root** é uma entidade que **controla o acesso ao seu agregado**.

`Cobranca` é o Aggregate Root. `Pagamento` faz parte do agregado de `Cobranca`.
Regras:
- Ninguém de fora cria `Pagamento` diretamente — apenas `Cobranca.RegistrarPagamento()`
- O construtor de `Pagamento` é `internal` — visível apenas dentro do projeto Domain
- O repositório salva/atualiza `Cobranca` inteira, não `Pagamento` individualmente

```csharp
// FORA do domínio — impossível criar Pagamento diretamente (internal)
var pagamento = new Pagamento(...); // ❌ CS0122: inaccessible due to protection level

// CORRETO — via Aggregate Root
var resultado = cobranca.RegistrarPagamento(valor, DateTimeOffset.UtcNow);
```

---

## 3. Value Objects

### Definição

**Value Object** é um objeto definido pelos seus atributos, sem identidade própria.
Se dois Value Objects têm os mesmos atributos, são **idênticos e intercambiáveis**.

### Por que não usar primitivos diretamente? (Primitive Obsession)

```csharp
// COM PRIMITIVE OBSESSION — bug silencioso
public Task ProcessarPagamento(string cpf, decimal valor, string moeda)
{ ... }

ProcessarPagamento(valor: 100m, cpf: "BRL", moeda: "529.982.247-25"); // Compilou! Bug em runtime.

// COM VALUE OBJECTS — erro em tempo de compilação
public Task ProcessarPagamento(Cpf cpf, Valor valor)
{ ... }

ProcessarPagamento(cpf: new Valor(100m), valor: new Cpf("...")); // ❌ CS1503: tipo errado
```

### Implementação: `Valor`

```csharp
var resultado = Valor.Criar(quantia: -50m); // Result.Falha("Quantia deve ser positiva")
var resultado = Valor.Criar(quantia: 100m); // Result.Ok(Valor { Quantia = 100m, Moeda = "BRL" })
var valor = resultado.Valor!;
valor.ToString(); // "R$ 100,00"
```

### Implementação: `Cpf`

Encapsula o algoritmo de validação de CPF (módulo 11):

```csharp
var resultado = Cpf.Criar("529.982.247-25"); // Result.Ok
var resultado = Cpf.Criar("123.456.789-00"); // Result.Falha("CPF inválido")
var cpf = resultado.Valor!;
cpf.Numero;     // "52998224725" (apenas dígitos)
cpf.Formatado;  // "529.982.247-25"
```

O algoritmo de validação:
1. Remover pontuação → apenas dígitos
2. Rejeitar CPFs com todos os dígitos iguais (ex: 111.111.111-11)
3. Calcular 1º dígito verificador: soma dos 9 primeiros × pesos (10..2) módulo 11
4. Calcular 2º dígito verificador: soma dos 10 primeiros × pesos (11..2) módulo 11

### Implementação: `CobrancaId`

Por que não usar `Guid` diretamente?

```csharp
// PROBLEMA com Guid puro:
Task<Cobranca?> BuscarPorIdAsync(Guid id);
// Qual tipo de ID é esse? Pode ser CobrancaId, PagamentoId, UsuarioId...

// COM CobrancaId:
Task<Cobranca?> BuscarPorIdAsync(CobrancaId id);
// Sem ambiguidade — compilador rejeita PagamentoId no lugar de CobrancaId
```

---

## 4. Domain Exceptions vs Result Pattern

### Exceções de domínio

Use para **condições excepcionais** que nunca deveriam ocorrer em operação normal:
- Invariante violada por bug de programação
- Estado inconsistente detectado

```csharp
// Exceção de domínio — estado inconsistente, bug de programação
public Cobranca(CobrancaId id, ...) : base(id.Valor)
{
    if (id.Valor == Guid.Empty)
        throw new InvalidOperationException("Id de cobrança não pode ser vazio.");
}
```

### Result Pattern

Use para **falhas esperadas** — casos onde o usuário pode ter fornecido dados inválidos:
- Validação de input
- Regras de negócio violadas pelo usuário (não por bug)

```csharp
// Result Pattern — falha esperada (usuário passou CPF errado)
var cpfResult = Cpf.Criar("123.456.000-99");
if (!cpfResult.Sucesso)
    return Result<ProcessarCobrancaResult>.Falha(cpfResult.MensagemErro!);
// Caller decide o que fazer — sem stack unwinding, sem try/catch
```

### Comparativo

| Situação | Usar |
|----------|------|
| CPF inválido fornecido pelo usuário | `Result.Falha(...)` |
| Cobrança já paga sendo paga novamente | `Result.Falha(...)` |
| ID Guid vazio passado por programação | `throw new InvalidOperationException(...)` |
| Dependência nula injetada | `ArgumentNullException.ThrowIfNull(...)` |

---

## 5. Interfaces de Domínio (Ports de Saída)

O domínio **define** as interfaces; a infraestrutura **implementa**.

### Por que a interface pertence ao domínio?

```
ERRADO (DIP violado):
  Domain ────────────────────────────────────► Infrastructure
  (usa Guid na busca)                         (define IRepositorioCobranca)

CORRETO (DIP respeitado):
  Domain (define IRepositorioCobranca) ◄────── Infrastructure
  (Domain não sabe que existe banco)           (implementa a interface)
```

### `IRepositorioCobranca` — Port de Saída

```csharp
// O domínio sabe que precisa "salvar uma Cobranca em algum lugar"
// Não sabe (nem quer saber) que esse "algum lugar" é Oracle, PostgreSQL ou memória
public interface IRepositorioCobranca
{
    Task<Cobranca?> BuscarPorIdAsync(CobrancaId id, CancellationToken ct = default);
    Task<IReadOnlyList<Cobranca>> ListarPorStatusAsync(StatusCobranca status, CancellationToken ct = default);
    Task SalvarAsync(Cobranca cobranca, CancellationToken ct = default);
    Task AtualizarAsync(Cobranca cobranca, CancellationToken ct = default);
}
```

### `INotificadorCobranca` — Port de Saída

```csharp
// O domínio sabe que precisa "notificar alguém"
// Não sabe se é SMTP, SendGrid, SMS ou push notification
public interface INotificadorCobranca
{
    Task NotificarAsync(string destinatario, string mensagem, CancellationToken ct = default);
}
```

Em arquitetura hexagonal, essas interfaces são chamadas de **Driven Ports** (Ports de Saída) —
portas que o domínio usa para interagir com o mundo externo, sem conhecer os detalhes.
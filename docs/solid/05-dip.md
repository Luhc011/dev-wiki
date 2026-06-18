# DIP — Princípio da Inversão de Dependência

## O que é?

Robert C. Martin define o DIP com duas regras:

> **Regra 1:** "Módulos de alto nível não devem depender de módulos de baixo nível.
> Ambos devem depender de abstrações."

> **Regra 2:** "Abstrações não devem depender de detalhes.
> Detalhes devem depender de abstrações."

## O que é "alto nível" vs "baixo nível"?

| Nível | Responsabilidade | Exemplos |
|-------|-----------------|---------|
| **Alto nível** | Regras de negócio, orquestração, casos de uso | `ServicoAutorizacaoPagamento`, `ProcessadorCobranca`, `CasoUsoTransferencia` |
| **Baixo nível** | Infraestrutura, I/O, banco de dados, rede, e-mail | `OracleRepositorio`, `SmtpNotificador`, `GatewayPixExterno`, `SistemaSerpro` |

O alto nível define **o que** o sistema faz. O baixo nível define **como** isso é feito.
O DIP diz que o **o que** não deve conhecer o **como**.

## O detalhe mais importante — Quem dono da abstração?

DIP não é apenas "use interfaces". É sobre **de qual lado da fronteira a interface mora**.

### Violação sutil (mas comum)

```
[Domínio]                    [Infraestrutura]
ServicoAutorizacao ──────→ IGatewayPix (definida aqui)
                                    ↑
                            GatewayPixBacen (implementa)
```

Neste caso, o negócio depende de algo definido pelo lado da infraestrutura.
A seta de dependência vai do negócio → infraestrutura: **DIP violado**.

### Correto — seta invertida

```
[Domínio]                    [Infraestrutura]
IGatewayPagamento ←──────── GatewayPixBacen (implementa)
(definida aqui)
        ↑
ServicoAutorizacao (usa)
```

A abstração **pertence ao domínio**. A infraestrutura depende do domínio (não o contrário).
A seta de dependência vai do domínio ← infraestrutura: **DIP respeitado**.

Esta distinção é o que diferencia DIP correto de apenas "usar interfaces".

## Como identificar violação de DIP

| Sinal | Descrição |
|-------|-----------|
| `new()` de infraestrutura em serviços de negócio | `private readonly OracleRepositorio _repo = new()` |
| `using` de namespace de infra em classes de domínio | `using MinhApp.Infra.Oracle` dentro de uma regra de negócio |
| Impossível testar sem banco, SMTP ou gateway real | `new ServicoAutorizacao()` precisaria de endpoint Serpro ativo |
| Strings de conexão ou endpoints no código de negócio | `private const string _endpoint = "https://serpro.gov.br/..."` |
| Construtor sem parâmetros em serviço de negócio | Impede injeção de dependências e mocks |

## A solução — Dependency Injection via construtor

```csharp
// VIOLAÇÃO — alto nível cria baixo nível:
public sealed class ServicoAutorizacaoPagamento
{
    private readonly SistemaSerpro   _antifraude  = new(); // 💥 Regra 1
    private readonly OracleRepositorio _repositorio = new(); // 💥 impossível trocar
    private readonly SmtpNotificador _notificador = new(); // 💥 impossível testar
}

// CORRETO — alto nível recebe abstrações do domínio:
public sealed class ServicoAutorizacaoPagamento(
    IAnalisadorAntifraude antifraude,   // domínio
    IGatewayPagamento     gateway,      // domínio
    IRepositorioPagamento repositorio,  // domínio
    INotificadorPagamento notificador)  // domínio
{ }
```

## Por que DIP torna os testes triviais

**Violação** — para testar `ServicoAutorizacaoPagamento` você precisaria de:
- Endpoint do Serpro ativo (latência + custo)
- Oracle com schema correto (ambiente de banco)
- Servidor SMTP configurado (conta de e-mail real)

Resultado: teste lento, frágil, dependente de ambiente, impossível em CI.

**Correto** — com abstrações injetadas via Moq:
```csharp
var antifraude  = new Mock<IAnalisadorAntifraude>();
var gateway     = new Mock<IGatewayPagamento>();
var repositorio = new Mock<IRepositorioPagamento>();
var notificador = new Mock<INotificadorPagamento>();

var sut = new ServicoAutorizacaoPagamento(
    antifraude.Object, gateway.Object, repositorio.Object, notificador.Object);
```

Resultado: teste roda em < 10ms, sem rede, sem banco, sem SMTP.
Qualquer cenário (aprovação, rejeição, falha do antifraude) é simulável.

## O container de DI — Microsoft.Extensions.DependencyInjection

O container de DI conecta abstrações com implementações concretas em runtime.
O serviço de negócio não sabe nem precisa saber qual implementação recebe.

```csharp
var services = new ServiceCollection();

// "Quando alguém pedir IAnalisadorAntifraude, dê AnalisadorAntifraudeSerpro"
services.AddScoped<IAnalisadorAntifraude, AnalisadorAntifraudeSerpro>();
services.AddScoped<IGatewayPagamento>(_ => new GatewayPixBacen { PrefixoTransacao = "PIX" });
services.AddScoped<IRepositorioPagamento, RepositorioPagamentoMemoria>();
services.AddScoped<INotificadorPagamento, NotificadorConsole>();
services.AddScoped<ServicoAutorizacaoPagamento>();

// Para trocar para outra implementação: UMA linha
// services.AddScoped<IAnalisadorAntifraude, AnalisadorAntifraudeClearSale>();
//                                           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                           ServicoAutorizacaoPagamento não é alterado
```

## Arquitetura Hexagonal — Ports & Adapters

DIP é o fundamento dos **Ports & Adapters** (Arquitetura Hexagonal):

```
           ┌─────────────────────────────────┐
           │           DOMÍNIO               │
           │                                 │
           │  ServicoAutorizacaoPagamento    │
           │                                 │
           │  [Port]         [Port]          │
           │  IAnalisador    IGateway        │  ← abstrações definidas aqui
           │  Antifraude     Pagamento       │
           └──────┬──────────────┬───────────┘
                  │              │
         ┌────────▼────┐   ┌─────▼──────────┐
         │  ADAPTADORES│   │   ADAPTADORES  │
         │             │   │                │
         │  Antifraude │   │  GatewayPix    │
         │  Serpro     │   │  Bacen         │
         └─────────────┘   └────────────────┘
                infraestrutura depende do domínio
```

A interface é o **Port** (domínio). A implementação concreta é o **Adapter** (infraestrutura).
O container de DI conecta Ports com Adapters em runtime.

## DIP fecha o ciclo SOLID

| Princípio | Contribuição para DIP |
|-----------|----------------------|
| **SRP** | Cada classe tem um motivo para mudar — fácil de injetar sem side effects |
| **OCP** | Novas implementações sem modificar o negócio — DIP torna possível |
| **LSP** | Qualquer implementação pode substituir a abstração — injeção funciona |
| **ISP** | Abstrações pequenas e precisas — fácil saber o que injetar |
| **DIP** | Fecha o ciclo: negócio não depende de infra — tudo é injetável |

Sem DIP, os outros quatro princípios perdem boa parte de sua utilidade prática:
abstrações bem desenhadas que não são injetáveis ainda geram acoplamento.
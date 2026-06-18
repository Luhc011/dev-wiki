# ISP — Princípio da Segregação de Interfaces

## O que é?

> "Os clientes não devem ser forçados a depender de interfaces que não usam."
> — Robert C. Martin

O ISP diz que interfaces grandes devem ser divididas em interfaces menores e mais específicas.
Cada cliente depende **apenas dos métodos que realmente usa** — sem métodos desnecessários,
sem `NotSupportedException` para satisfazer contratos irrelevantes.

## Por que importa?

Quando uma interface é gorda (fat interface), todos os seus implementadores são forçados a:

1. Implementar métodos que não fazem sentido para eles
2. Lançar `NotSupportedException` ou `throw new Exception("Não suportado")`
3. Retornar valores nulos ou padrão sem significado
4. Ser recompilados quando qualquer parte da interface muda

Isso viola LSP (subtipos que lançam `NotSupportedException` não substituem a base),
OCP (clientes que verificam tipo concreto para decidir o que chamar), e
SRP (interfaces que agrupam responsabilidades não relacionadas).

## Exemplo 1 — Contas Bancárias

### Violação: Interface gorda `IContaBancaria`

```
IContaBancaria (8 métodos)
  ├── ConsultarSaldoAsync()       ← faz sentido para todos
  ├── DepositarAsync()            ← faz sentido para todos
  ├── SacarAsync()               ← NÃO faz sentido para Conta Investimento
  ├── TransferirAsync()          ← NÃO faz sentido para Conta Investimento
  ├── EmitirCartaoAsync()        ← NÃO faz sentido para Conta Investimento nem Digital
  ├── BloquearCartaoAsync()      ← NÃO faz sentido para Conta Investimento nem Digital
  ├── AplicarRendimentoAsync()   ← NÃO faz sentido para Conta Corrente nem Digital
  └── ResgatarAsync()            ← NÃO faz sentido para Conta Corrente nem Digital
```

**Resultado:** 3 classes com `NotSupportedException` em métodos que não fazem sentido para elas.

### Correto: Interfaces segregadas por capacidade

```
IContaBancaria (base — 2 métodos para TODOS)
  ├── ConsultarSaldoAsync()
  └── DepositarAsync()

IContaSacavel : IContaBancaria
  └── SacarAsync()

IContaTransferivel : IContaBancaria
  └── TransferirAsync()

IContaRendimento : IContaBancaria
  ├── AplicarRendimentoAsync()
  └── ResgatarAsync()

IContaComCartao : IContaBancaria
  ├── EmitirCartaoAsync()
  └── BloquearCartaoAsync()
```

**Implementações:**
- `ContaCorrente` → implementa `IContaSacavel + IContaTransferivel + IContaComCartao`
- `ContaInvestimento` → implementa apenas `IContaRendimento`
- `ContaDigital` → implementa `IContaSacavel + IContaTransferivel`

**Resultado:** Zero `NotSupportedException`. O compilador impede chamadas inválidas.

## Exemplo 2 — Repositório de Cobranças

### Violação: Interface gorda `IRepositorioCobranca`

```
IRepositorioCobranca (6 métodos — mistura 5 responsabilidades)
  ├── BuscarPorIdAsync()            ← persistência/leitura
  ├── ListarPorStatusAsync()        ← persistência/leitura
  ├── SalvarAsync()                 ← persistência/escrita
  ├── GerarRelatorioAsync()         ← relatório
  ├── ExportarCsvAsync()            ← exportação
  └── EnviarNotificacoesPendentesAsync() ← notificação
```

**Problema:** `ServicoConsultaCobranca` usa **apenas** `BuscarPorIdAsync` e `ListarPorStatusAsync`,
mas depende da interface inteira. Qualquer mudança em `GerarRelatorioAsync` força recompilação do
serviço de consulta, mesmo que ele nunca chame esse método.

### Correto: Interfaces por responsabilidade

```
IRepositorioCobrancaLeitura   → BuscarPorIdAsync + ListarPorStatusAsync
IRepositorioCobrancaEscrita   → SalvarAsync
IRelatorioCobrancaService     → GerarAsync
IExportadorCobranca           → ExportarCsvAsync
INotificadorCobranca          → NotificarPendentesAsync
```

**Resultado:** `ServicoConsultaCobrancaCorreto` depende apenas de `IRepositorioCobrancaLeitura`
(2 métodos). Mudanças em exportação, relatório ou notificação não o afetam.

## ISP e LSP — Relação direta

ISP correto → LSP automaticamente respeitado.

Quando cada interface só exige o que o implementador pode honrar:
- Não há motivo para `NotSupportedException`
- Subtipos substituem a base sem surpresas
- Callers não precisam de `try/catch` defensivo

## Sinais de ISP violado

| Sinal | Descrição |
|-------|-----------|
| `NotSupportedException` em `override` | Método obrigatório pela interface mas sem sentido na classe |
| `return null!` ou `return ""` | Implementação vazia para satisfazer assinatura |
| `if (obj is ConcreteType)` no caller | Caller verifica tipo para decidir o que é seguro chamar |
| Mock com muitos `.Setup()` ignorados | No teste, mais métodos configurados do que os realmente usados |
| Classe muda sem relação com o cliente | Mudança em parte da interface força recompilação de quem não usa aquela parte |

## Regra prática

Se você está escrevendo `throw new NotSupportedException()` em um `override`,
pergunte: "Essa operação deveria estar em uma interface separada?"

Se a resposta for sim — refatore antes que o `NotSupportedException` vire um bug em produção.
# LSP — Princípio de Substituição de Liskov

## Definição formal

> "Se S é um subtipo de T, então objetos do tipo T podem ser substituídos por objetos do tipo S
> sem alterar nenhuma das propriedades desejáveis do programa."
> — Barbara Liskov, OOPSLA 1987

Em termos práticos:

> Qualquer código que funciona com a classe base deve funcionar igualmente bem com qualquer subtipo —
> sem condicionais de tipo, sem surpresas de comportamento, sem exceções não previstas.

Se você substituir mentalmente a base por um subtipo e o programa se comportar diferente do esperado,
o LSP está violado.

---

## Os 3 contratos formais

O LSP é garantido quando os subtipos respeitam três regras de contrato em relação ao supertipo:

### 1. Pré-condições não podem ser fortalecidas

**Pré-condição** = o que o método exige do caller para funcionar.

O subtipo não pode exigir mais do caller do que a base exige.
Se a base aceita `SolicitacaoPagamento` com qualquer horário, o subtipo não pode rejeitar
solicitações fora do horário bancário — isso fortalece a pré-condição.

**Direção permitida:** afrouxar (aceitar mais do que a base aceita).

### 2. Pós-condições não podem ser enfraquecidas

**Pós-condição** = o que o método garante ao caller como resultado.

O subtipo não pode entregar menos do que a base promete.
Se a base garante que `ResultadoPagamento.IdTransacao` nunca é nulo, o subtipo não pode
retornar nulo — isso enfraquece a pós-condição.

**Direção permitida:** reforçar (garantir mais do que a base garante).

### 3. Invariantes devem ser mantidas

**Invariante** = propriedade do objeto que nunca deve ser violada.

O subtipo não pode introduzir estados que a base garantia impossíveis.
Se a base garante que `Comprovante` é sempre preenchido após processamento bem-sucedido,
o subtipo não pode retornar `null`.

**Direção permitida:** expandir (garantir ainda mais estados consistentes).

---

## Direção permitida — resumo

| Contrato | Permitido no subtipo | Proibido no subtipo |
|----------|---------------------|---------------------|
| Pré-condição | Afrouxar (aceitar mais) | Fortalecer (exigir mais) |
| Pós-condição | Reforçar (entregar mais) | Enfraquecer (entregar menos) |
| Invariante | Expandir (garantir mais) | Quebrar (violar a garantia) |

---

## Sinais de violação

| Anti-pattern | O que indica |
|-------------|-------------|
| `NotSupportedException` / `NotImplementedException` em override | Subtipo não honra o contrato |
| `if (x is SubTipoConcreto)` no caller | Caller acumulou conhecimento do subtipo |
| `switch (x) { case SubTipo... }` no caller | Idem |
| Método no-op (`override` que não faz nada) | Subtipo ignora o contrato silenciosamente |
| Comentário: "não use X com Y" | Acoplamento implícito não garantido pelo tipo |
| `try/catch NotSupportedException` no caller | O caller sabe que o contrato pode ser quebrado |
| Null check defensivo no caller para resultado não-nulo | Subtipo quebra pós-condição de não-nulidade |

---

## O Liskov Test

Substitua mentalmente a base por cada subtipo e execute o código cliente na cabeça.
Se qualquer subtipo produz comportamento diferente ou inesperado, o LSP está violado.

Na prática: **todos os testes escritos para o supertipo devem passar sem alteração
quando qualquer subtipo é usado no lugar.**

```csharp
// Liskov Test como teste automatizado:
[Theory]
[InlineData(typeof(ProcessadorPix))]
[InlineData(typeof(ProcessadorBoleto))]
[InlineData(typeof(ProcessadorTed))]
public async Task ProcessarAsync_QualquerImplementacao_DeveHonrarContrato(Type tipo)
{
    var processador = (IProcessadorPagamento)Activator.CreateInstance(tipo)!;
    var resultado = await processador.ProcessarAsync(solicitacao);

    // Se qualquer subtipo falhar aqui, LSP está violado
    resultado.IdTransacao.Should().NotBeNullOrEmpty();
    resultado.Status.Should().NotBe(StatusPagamento.Indefinido);
}
```

---

## Exemplo no domínio bancário

### A interface gorda — raiz das violações

```csharp
// 🚨 Interface com 4 métodos que nem todos os processadores conseguem honrar
public interface IProcessadorPagamento
{
    Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento s);
    Task<Comprovante> GerarComprovanteAsync(string idTransacao);  // Boleto não tem comprovante imediato
    Task CancelarAsync(string idTransacao);                       // Boleto e TED não podem cancelar
    Task<StatusPagamento> ConsultarStatusAsync(string idTransacao);
}
```

**Resultado:** `ProcessadorBoleto` e `ProcessadorTed` são forçados a implementar
métodos que não conseguem honrar, quebrando o LSP com `NotSupportedException`.

### A solução — interfaces segregadas por capacidade

```csharp
// Base: o que TODO processador pode honrar
public interface IProcessadorPagamento
{
    Task<ResultadoPagamento> ProcessarAsync(SolicitacaoPagamento s);
    Task<StatusPagamento> ConsultarStatusAsync(string idTransacao);
}

// Capacidade opcional: apenas quem pode, implementa
public interface IProcessadorCancelavel      : IProcessadorPagamento { Task CancelarAsync(string id); }
public interface IProcessadorComComprovante  : IProcessadorPagamento { Task<Comprovante> GerarComprovanteAsync(string id); }
public interface IProcessadorComJanela       : IProcessadorPagamento { bool EstaDisponivel(DateTimeOffset agora); }
```

**PIX** implementa `IProcessadorCancelavel` + `IProcessadorComComprovante` (24/7, cancela, tem comprovante).
**Boleto** implementa apenas `IProcessadorPagamento` (não cancela, sem comprovante imediato).
**TED** implementa `IProcessadorCancelavel` + `IProcessadorComComprovante` + `IProcessadorComJanela`.

O compilador garante que `ProcessadorBoleto` nunca chega num contexto que exige
`IProcessadorCancelavel` — sem `NotSupportedException`, sem surpresas em runtime.

---

## Relação com ISP

Violações de LSP revelam **interfaces gordas**: quando um subtipo não consegue honrar todos os
métodos da interface, é sinal de que a interface agrega responsabilidades que não pertencem juntas.

ISP resolve a raiz do problema: interfaces segregadas permitem que cada tipo implemente apenas
o que consegue honrar. LSP deixa de ser violado porque o contrato só inclui o que é possível.

> LSP diagnostica o sintoma; ISP cura a causa.

---

## Relação com OCP

OCP só funciona se os novos subtipos forem substituíveis. Ao adicionar `ProcessadorPix` como
extensão (OCP), o sistema só permanece correto se `ProcessadorPix` honrar todos os contratos
do supertipo (LSP).

> OCP abre a porta para extensão; LSP garante que a extensão não quebra o que existe.

---

## Código de referência

- **Violação:** `src/Solid/DevWiki.Solid.Lsp/Violation/`
- **Correto:** `src/Solid/DevWiki.Solid.Lsp/Correct/`
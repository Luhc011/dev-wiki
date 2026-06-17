# OCP — Princípio Aberto/Fechado

## Definição original

> "Módulos de software devem ser abertos para extensão e fechados para modificação."
> — Bertrand Meyer, *Object-Oriented Software Construction* (1988)

Reformulação de Robert C. Martin, mais aplicável à prática:

> "Um artefato de software deve ser aberto para extensão de comportamento e fechado para modificação de código já escrito."

---

## O que significa "aberto para extensão"

Novo comportamento é adicionado **escrevendo código novo** — uma nova classe, um novo método de extensão, uma nova implementação de interface — sem precisar alterar o que já existe.

No domínio bancário: se precisamos suportar um novo tipo de pagamento "DOC", criamos `TarifaDoc : ICalculadoraTarifa`. O `ServicoTarifa` existente e todas as outras tarifas **não são tocadas**.

## O que significa "fechado para modificação"

Código que já foi escrito, testado e está em produção **não é alterado** para acomodar novos casos de uso. A interface pública e o comportamento de classes existentes permanece estável.

Isso não significa que código nunca muda — significa que *adicionar um novo tipo de cobrança* não deveria exigir abrir o `CalculadoraTarifa.cs` que já funciona.

---

## Por que violar OCP é perigoso

Cada modificação em código que já existe é um risco:

1. **Risco de regressão:** alterar um `switch/if` para adicionar um novo caso pode quebrar os casos que já funcionavam
2. **Risco de merge conflict:** times diferentes tocando o mesmo arquivo simultaneamente
3. **Risco de cobertura:** testes existentes verificam os casos antigos, mas o novo caso pode ter interações inesperadas com a lógica original
4. **Escalabilidade zero:** quanto mais tipos existirem, mais o switch cresce e mais perigoso fica

### O custo escala com o tamanho

Com 3 tipos de pagamento, um switch tem 3 casos — gerenciável.
Com 12 tipos, o mesmo switch tem 12 casos, potencialmente em um arquivo com 300+ linhas.
Cada adição é proporcional ao risco total acumulado.

---

## O gatilho da violação

O sinal mais claro de violação do OCP é o **switch ou if baseado em tipo/string** para decidir comportamento:

```csharp
// 🚨 Sinal de OCP violado — toda adição nova exige modificar aqui
public decimal Calcular(string tipo, decimal valor) => tipo switch
{
    "PIX"    => 0m,
    "TED"    => Math.Max(5.00m, valor * 0.002m),
    "Boleto" => 2.50m,
    // 💥 para adicionar DOC, o desenvolvedor ABRE ESTE ARQUIVO
    _        => throw new ArgumentException("Tipo desconhecido")
};
```

A pergunta diagnóstico: **"toda vez que adicionar um novo X, preciso modificar a classe Y?"**

Se a resposta for sim, Y viola o OCP em relação a X.

---

## A solução: abstrações + Strategy pattern

Em vez de um switch central, cada variação vira uma implementação de uma abstração:

```csharp
// Abstração estável — fechada para modificação
public interface ICalculadoraTarifa
{
    string TipoPagamento { get; }
    decimal Calcular(decimal valor);
}

// Implementações abertas para adição
public sealed class TarifaPix    : ICalculadoraTarifa { ... }
public sealed class TarifaTed    : ICalculadoraTarifa { ... }
public sealed class TarifaBoleto : ICalculadoraTarifa { ... }
public sealed class TarifaDoc    : ICalculadoraTarifa { ... }  // adicionada sem tocar nas outras

// Serviço fechado para modificação: recebe qualquer conjunto de calculadoras
public sealed class ServicoTarifa(IEnumerable<ICalculadoraTarifa> calculadoras)
{
    private readonly Dictionary<string, ICalculadoraTarifa> _mapa =
        calculadoras.ToDictionary(c => c.TipoPagamento);

    public decimal Calcular(string tipo, decimal valor)
        => _mapa.TryGetValue(tipo, out var c) ? c.Calcular(valor)
           : throw new ArgumentException($"Tipo '{tipo}' não registrado.", nameof(tipo));
}
```

Para adicionar `TarifaTevNoturno`: crie a classe, registre no container de DI. Zero modificação em `ServicoTarifa` ou nas outras tarifas.

---

## Relação com LSP

OCP só funciona se as novas implementações honrarem o contrato da abstração. Se `TarifaDoc.Calcular` retornar valor negativo quando o contrato diz que o resultado é sempre ≥ 0, o sistema quebra mesmo sem modificar o `ServicoTarifa`.

> OCP abre a porta; LSP garante que quem entra não destrói a casa.

---

## Relação com DIP

OCP depende fundamentalmente de **depender de abstrações, não de concretos**. O `ServicoTarifa` que aceita `IEnumerable<ICalculadoraTarifa>` pode ser estendido porque depende da interface, não das classes. Se dependesse de `TarifaPix` e `TarifaTed` diretamente, cada nova tarifa exigiria modificação.

> DIP cria os trilhos; OCP faz o trem andar.

---

## Exemplos no domínio bancário

### Violação — tarifas

```csharp
// VIOLA OCP: adicionar DOC exige mexer aqui
public sealed class CalculadoraTarifa
{
    public decimal Calcular(string tipoPagamento, decimal valor) => tipoPagamento switch
    {
        "PIX"    => 0m,
        "TED"    => Math.Max(5.00m, valor * 0.002m),
        "Boleto" => 2.50m,
        _        => throw new ArgumentException(nameof(tipoPagamento))
    };
}
```

### Correto — tarifas

```csharp
// Aberto para extensão: nova implementação, zero modificação
public sealed class TarifaDoc : ICalculadoraTarifa
{
    public string TipoPagamento => "DOC";
    public decimal Calcular(decimal valor) => Math.Max(8.00m, valor * 0.003m);
}
```

### Violação — relatórios

```csharp
// VIOLA OCP: adicionar JSON exige abrir este arquivo
public string Gerar(RelatorioCobranca r, string formato) => formato switch
{
    "TXT" => GerarTxt(r),
    "CSV" => GerarCsv(r),
    _     => throw new ArgumentException(nameof(formato))
};
```

### Correto — relatórios

```csharp
// Adicionado sem modificar FormatadorTxt nem FormatadorCsv
public sealed class FormatadorJson : IFormatadorRelatorio
{
    public string Formato => "JSON";
    public string Formatar(RelatorioCobranca r) { ... }
}
```

---

## Benefícios observáveis

| Cenário | Com violação | Com OCP |
|---------|-------------|---------|
| Adicionar tipo de pagamento | Modificar classe existente, retestes de tudo | Nova classe, zero regressão |
| Testes de novo tipo | Acoplados ao switch | Isolados na nova classe |
| Deploy de nova funcionalidade | Recompilar módulo inteiro | Apenas a nova classe + injeção |
| Paralelismo de times | Conflito de merge no switch | Times independentes |

---

## Código de referência

- **Violação:** `src/Solid/DevWiki.Solid.Ocp/Violacao/`
- **Correto:** `src/Solid/DevWiki.Solid.Ocp/Correto/`
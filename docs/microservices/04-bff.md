# BFF — Backend for Frontend

## 1. O problema de uma única API para múltiplos clientes

### Clientes diferentes têm necessidades radicalmente diferentes

**Mobile** (app iOS/Android):
- Banda limitada — cada byte conta
- Bateria — processar dados custa energia
- Tela pequena — exibe poucos campos
- Precisa de respostas compactas, paginação agressiva

**Web** (browser desktop):
- Banda ampla — pode receber dados ricos
- Precisa de dados completos para montar a página
- Pode exibir tabelas complexas, histórico detalhado

**Parceiro B2B** (integração de sistema para sistema):
- Precisa de campos específicos para o contrato de integração
- Não precisa de formatação para exibição humana
- Pode precisar de campos que o app mobile nunca usa

### A armadilha da API genérica

```csharp
// Uma API que tenta atender todos:
GET /cobrancas/{id}
→ retorna: id, cpf, valor, descricao, status, pagamentos[], 
           logs[], auditoria[], histórico[], configurações[]
```

Resultado:
- **Mobile**: recebe 100 campos mas usa 5 — desperdício de banda e CPU
- **Web**: recebe tudo — ok, mas mistura dados de contextos diferentes
- **Parceiro**: precisa de `idExterno` e `codigoRetorno` que não existem na resposta
- **Equipe**: não consegue evoluir a API sem quebrar clientes

---

## 2. BFF — Backend for Frontend

### Definição

BFF é uma **API dedicada para um tipo específico de cliente**, que:
1. Recebe requisições do cliente
2. Agrega dados de múltiplos serviços internos
3. Formata a resposta especificamente para aquele cliente
4. Evolui independentemente dos outros BFFs

```
                        ┌─────────────────────┐
  [App Mobile] ────────►│    BFF Mobile       │
                        │  resposta compacta  │───┐
                        └─────────────────────┘   │
                                                   ▼
                        ┌─────────────────────┐  ServiçoCobrança
  [Browser Web] ───────►│    BFF Web          │  ServiçoPagamento
                        │  resposta completa  │───┤  ServiçoAntifraude
                        └─────────────────────┘   │  (serviços internos)
                                                   │
                        ┌─────────────────────┐   │
  [Sistema Parceiro] ──►│    BFF Parceiro     │───┘
                        │  campos específicos │
                        └─────────────────────┘
```

### BFF não é um proxy burro

```csharp
// ❌ PROXY BURRO — apenas repassa chamadas
public async Task<IActionResult> Get(Guid id)
{
    var response = await _httpClient.GetAsync($"/cobrancas/{id}");
    return Content(await response.Content.ReadAsStringAsync());
}

// ✅ BFF REAL — agrega, transforma, adapta
public async Task<CobrancaWebResponse?> BuscarAsync(Guid id, CancellationToken ct)
{
    // Chama dois serviços internos em paralelo
    var (cobranca, pagamentos) = await (
        _servicoCobranca.BuscarCobrancaAsync(id, ct),
        _servicoPagamento.BuscarPagamentosAsync(id, ct)
    ).WhenAll();
    
    if (cobranca is null) return null;
    
    // Formata especificamente para o cliente Web
    return new CobrancaWebResponse(
        Id: cobranca.Id,
        CpfDevedor: FormatarCpf(cobranca.CpfDevedor),
        ValorFormatado: $"R$ {cobranca.Valor:N2}",
        StatusDescritivo: cobranca.Status.ToDescricao(),
        HistoricoPagamentos: [.. pagamentos.Select(MapearPagamento)]
    );
}
```

---

## 3. Vantagens e desvantagens

### Vantagens

| Vantagem | Descrição |
|----------|-----------|
| **API otimizada por cliente** | Mobile recebe exatamente o que precisa |
| **Times independentes** | Time Mobile pode evoluir BFF Mobile sem consultar time Web |
| **Evolução independente** | Mudar o BFF Mobile não quebra o BFF Web |
| **Segurança por contexto** | BFF Parceiro pode expor apenas campos permitidos por contrato |
| **Cache por contexto** | Mobile pode ter cache agressivo; Web pode precisar de dados frescos |

### Desvantagens

| Desvantagem | Descrição |
|-------------|-----------|
| **Duplicação** | Lógica de chamar ServiçoCobrança existe em todos os BFFs |
| **Mais serviços** | 3 BFFs = mais 3 serviços para deployar, monitorar, escalar |
| **Consistência** | Se a lógica compartilhada muda, precisa atualizar todos os BFFs |

### Quando usar

✅ Use BFF quando:
- Há múltiplos tipos de cliente com necessidades muito diferentes
- Times diferentes desenvolvem cada experiência de usuário
- A "API genérica" está crescendo caoticamente para servir todos

❌ Evite BFF quando:
- Há apenas um tipo de cliente
- Os clientes têm necessidades muito parecidas (um campo a mais não justifica um BFF)
- A equipe é pequena e não consegue manter N serviços

---

## 4. BFF vs API Gateway

A confusão entre os dois é comum:

| Aspecto | API Gateway | BFF |
|---------|------------|-----|
| **Responsabilidade** | Roteamento, autenticação, rate limiting, TLS termination | Composição de dados e adaptação por tipo de cliente |
| **Conhece o domínio?** | Não — transparente | Sim — sabe o que o cliente precisa |
| **Lógica de negócio?** | Não | Lógica de composição (não de negócio) |
| **Um por...** | Organização / ambiente | Tipo de cliente |
| **Exemplos** | Kong, AWS API Gateway, nginx | Este módulo — BffMobile, BffWeb, BffParceiro |

### Coexistência

```
[Mobile] ─► [API Gateway] ─► [BFF Mobile] ─► ServiçoCobrança
                              ─► ServiçoPagamento

[Web]    ─► [API Gateway] ─► [BFF Web]    ─► ServiçoCobrança
                              ─► ServiçoPagamento
                              ─► ServiçoAntifraude
```

O API Gateway cuida de cross-cutting concerns (auth, rate limit, logging).
O BFF cuida da composição específica por tipo de cliente.

---

## 5. Implementação neste módulo

```csharp
// BffMobile: compacto, sem histórico, sem pagamentos detalhados
public sealed record CobrancaMobileResponse(
    Guid Id,
    decimal Valor,
    string Status);      // apenas 3 campos

// BffWeb: completo, com pagamentos
public sealed record CobrancaWebResponse(
    Guid Id,
    string CpfDevedor,
    decimal Valor,
    string Descricao,
    DateOnly DataVencimento,
    string Status,
    IReadOnlyList<PagamentoDetalhe> Pagamentos);  // tudo

// BffParceiro: campos específicos para integração B2B
public sealed record CobrancaParceiroResponse(
    string IdExterno,    // Guid convertido para string com prefixo
    string Situacao);    // código de status para o sistema parceiro
```

Cada BFF implementa a mesma interface (`IBff<TResponse>`) mas retorna tipos
completamente diferentes — otimizados para seu cliente específico.
# SRP — Princípio da Responsabilidade Única

## Definição original

> "Uma classe deve ter apenas um motivo para mudar."
> — Robert C. Martin, *Clean Architecture* (2017)

A formulação mais precisa, do próprio Martin:

> "Reúna as coisas que mudam pelos mesmos motivos. Separe as coisas que mudam por motivos diferentes."

---

## O que é "responsabilidade" no contexto do SRP

O nome "Responsabilidade Única" induz a um erro comum: imaginar que uma classe deve fazer **uma única coisa** — um único método, ou um único nível de abstração. Esse não é o significado do princípio.

**Responsabilidade = motivo de mudança = ator que requisita a mudança.**

Um **ator** é uma pessoa, papel ou grupo de stakeholders que tem autoridade para exigir que aquela classe se comporte de forma diferente. Quando dois atores distintos precisam mexer na mesma classe, SRP está violado — porque uma mudança feita para atender um ator pode quebrar o que o outro ator precisava.

### Exemplo concreto

Imagine uma classe `ServicoCobranca` que:
- valida regras de negócio (ator: **time de pagamentos**)
- formata e-mails de notificação (ator: **time de notificações**)
- grava no banco de dados (ator: **time de dados/DBA**)
- gera relatórios em texto (ator: **time de relatórios**)
- aplica regras de compliance (ator: **time jurídico/compliance**)

Quando o time de notificações quer mudar o template do e-mail, é obrigado a abrir a mesma classe onde vivem as regras de validação de pagamento. Qualquer erro de merge, conflito de branch ou teste quebrado afeta **os cinco times ao mesmo tempo**.

---

## Como identificar múltiplas responsabilidades

A pergunta-chave é: **"Quem pode pedir mudança nessa classe?"**

Se a resposta tiver mais de um stakeholder/ator, SRP está violado.

### Sinais de alerta

| Sintoma | O que indica |
|---------|-------------|
| Classe com mais de ~200 linhas | Provavelmente agrega múltiplas responsabilidades |
| Métodos com nomes genéricos: `Processar`, `Executar`, `Gerenciar` | Verbo genérico esconde múltiplos comportamentos |
| Muitos `using` de namespaces não relacionados | A classe depende de domínios distintos |
| Testes que precisam mockar dependências de áreas diferentes | O SUT depende de infraestruturas não relacionadas |
| Dificuldade de nomear a classe com um substantivo preciso | A classe não tem identidade única |
| Método privado que contém lógica de negócio não testável | A responsabilidade está escondida, não isolada |

---

## Exemplo no domínio bancário

### Violação

```csharp
// VIOLA SRP: cinco atores distintos podem requisitar mudanças aqui
public class ServicoCobranca
{
    // Time de compliance exige essa validação
    private bool ValidarSolicitacao(SolicitacaoPagamento s) { ... }

    // Time de pagamentos define esse fluxo
    public async Task<ResultadoPagamento> ProcessarCobrancaAsync(SolicitacaoPagamento s) { ... }

    // Time de dados/DBA controla esse método
    private async Task SalvarNoBancoAsync(ResultadoPagamento resultado) { ... }

    // Time de notificações é dono desse template
    private async Task EnviarNotificacaoAsync(string email, ResultadoPagamento r) { ... }

    // Time de relatórios formata esse output
    public string GerarRelatorioTexto(IEnumerable<ResultadoPagamento> resultados) { ... }
}
```

**Problema real:** Se o time jurídico exige uma nova regra de validação na véspera do release, o time de pagamentos precisa revisar o mesmo arquivo. O time de notificações, ao mudar o template de e-mail, pode introduzir um bug na lógica de persistência durante o merge.

### Aplicação correta do SRP

```csharp
// Cada classe tem um único ator
public class ValidadorSolicitacao    : IValidadorSolicitacao { ... }  // dono: compliance
public class RepositorioPagamento    : IRepositorioPagamento { ... }  // dono: time de dados
public class NotificacaoService      : INotificacaoService   { ... }  // dono: notificações
public class RelatorioService        : IRelatorioService     { ... }  // dono: relatórios
public class ProcessadorCobranca     : IProcessadorCobranca  { ... }  // dono: pagamentos
```

`ProcessadorCobranca` **orquestra** sem **implementar**: recebe as quatro dependências via construtor e coordena o fluxo. Mudanças em qualquer área afetam apenas a classe correspondente.

---

## Relação com coesão e acoplamento

**Coesão** mede o quanto os elementos de um módulo pertencem juntos.
**Acoplamento** mede o quanto um módulo depende de outros.

O SRP **maximiza a coesão** ao garantir que tudo dentro de uma classe existe pelo mesmo motivo de mudança. Como consequência, **minimiza o acoplamento acidental**: classes pequenas e focadas tendem a depender apenas do que realmente precisam.

A relação é direta:

```
Alta coesão (SRP respeitado) → Baixo acoplamento acidental
Baixa coesão (SRP violado)   → Alto acoplamento acidental
```

---

## SRP não significa "uma classe, um método"

Estes equívocos são comuns:

| Equívoco | Realidade |
|----------|-----------|
| "SRP = apenas um método por classe" | Errado. Uma classe pode ter vários métodos se todos servem ao mesmo ator |
| "SRP = máxima granularidade" | Errado. Fragmentar demais cria acoplamento estrutural desnecessário |
| "SRP = separar por camadas técnicas" | Incompleto. A separação deve seguir os **atores de negócio**, não apenas a arquitetura |

**Exemplo válido de múltiplos métodos na mesma classe:**

```csharp
// Todos os métodos têm um único ator: time de compliance
public class ValidadorSolicitacao
{
    public ResultadoValidacao Validar(SolicitacaoPagamento s) { ... }
    private bool ValorPositivo(decimal valor) { ... }
    private bool BancoOrigemPreenchido(string banco) { ... }
    private bool DescricaoNaoVazia(string descricao) { ... }
}
```

---

## Benefícios observáveis

1. **Testabilidade:** cada responsabilidade pode ser testada em isolamento, sem mockar dependências não relacionadas
2. **Legibilidade:** o nome da classe diz exatamente o que ela faz — sem ambiguidade
3. **Paralelismo de equipes:** times distintos trabalham em classes distintas sem conflitos de merge
4. **Manutenção:** uma mudança de negócio afeta apenas a classe relevante — sem surpresas

---

## Código de referência

- **Violação:** `src/Solid/DevWiki.Solid.Srp/Violacao/ServicoCobranca.cs`
- **Correto:** `src/Solid/DevWiki.Solid.Srp/Correto/`
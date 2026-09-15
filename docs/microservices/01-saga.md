# Saga Pattern

## 1. O problema que o Saga resolve

### Transações ACID em um único banco

Em um monolito com um único banco de dados, uma operação complexa é simples:

```sql
BEGIN TRANSACTION;
  -- Passo 1: verificar antifraude (tabela local)
  UPDATE cobranças SET status = 'em_analise' WHERE id = ?;
  -- Passo 2: debitar conta
  UPDATE contas SET saldo = saldo - ? WHERE cpf = ?;
  -- Passo 3: registrar notificação
  INSERT INTO notificações (cobranca_id, mensagem) VALUES (?, ?);
COMMIT; -- tudo junto ou nada
```

Se qualquer passo falhar, o `ROLLBACK` desfaz tudo. O banco garante atomicidade.

### O problema em microsserviços

Com três serviços independentes (cada um com seu banco):

```
ServiçoAntifraude  ──→  banco_antifraude
ServiçoDebito      ──→  banco_debito
ServiçoNotificação ──→  banco_notificacao
```

**Não existe `BEGIN TRANSACTION` entre bancos diferentes.**

Cenário de falha:
1. ✅ ServiçoAntifraude aprova a cobrança
2. ✅ ServiçoDebito debita R$ 500 da conta
3. ❌ ServiçoNotificação falha — servidor de e-mail fora do ar

Resultado: dinheiro debitado, cliente não notificado. Sistema inconsistente.

Como desfazer o débito se não há transação global? Essa é a pergunta que o Saga responde.

---

## 2. O que é Saga?

Saga (Hector Garcia-Molina, 1987) é uma **sequência de transações locais** onde
cada transação publica um evento ou envia uma mensagem para disparar a próxima.

Se um passo falha, a Saga executa **transações compensatórias** para desfazer
semanticamente o que os passos anteriores fizeram.

**Ponto crucial**: a compensação não é um `ROLLBACK` SQL. É uma **nova operação
de negócio** que inverte o efeito. O dinheiro já foi debitado — a compensação
cria uma nova operação de crédito de volta.

---

## 3. Saga Choreography (Coreografia)

### Definição

Na coreografia, **cada serviço reage a eventos** publicados por outros. Não há
um orquestrador central — os serviços são coreografados como dançarinos que
conhecem seus passos sem um regente.

### Fluxo de sucesso

```
CobrançaCriada
      │
      ▼
ServiçoAntifraude
  analisa...
      │
      ▼ publica
AntifraudeAprovado
      │
      ▼
ServiçoDebito
  debita...
      │
      ▼ publica
DebitoRealizado
      │
      ▼
ServiçoNotificação
  notifica...
      │
      ▼ publica
NotificaçãoEnviada ──── FIM (sucesso)
```

### Fluxo de compensação (NotificaçãoFalhou)

```
DebitoRealizado
      │
      ▼
ServiçoNotificação
  falha!
      │
      ▼ publica
NotificaçãoFalhou
      │
      ▼
ServiçoDebito (ouve NotificaçãoFalhou)
  CancelarDebitoAsync(...)  ← operação de compensação
      │
      ▼ publica
DebitoCancelado ──── FIM (compensado)
```

### Vantagens da Choreography

- **Baixo acoplamento**: cada serviço só conhece os eventos, não os outros serviços
- **Sem ponto central de falha**: o orquestrador não existe para cair
- **Fácil de adicionar serviços**: um novo serviço se inscreve nos eventos existentes

### Desvantagens da Choreography

- **"Coreografia caótica"**: em sistemas complexos, o fluxo total é difícil de visualizar
- **Lógica espalhada**: cada serviço contém um pedaço do fluxo total
- **Difícil de debugar**: rastrear um problema exige olhar logs de múltiplos serviços
- **Ciclos acidentais**: um serviço pode inadvertidamente reagir a seus próprios eventos

---

## 4. Saga Orchestration (Orquestração)

### Definição

Na orquestração, um **SagaOrchestrator centralizado** conhece todos os passos e
coordena a execução. Os serviços individuais não sabem que fazem parte de uma Saga
— eles recebem comandos e respondem com sucesso ou falha.

### Fluxo de sucesso

```
SagaOrchestrator
  ├── Passo 1: envia para ServiçoAntifraude → resposta: aprovado
  ├── Passo 2: envia para ServiçoDebito     → resposta: debitado
  └── Passo 3: envia para ServiçoNotificação → resposta: enviado
  └── Saga concluída
```

### Fluxo de compensação

```
SagaOrchestrator
  ├── Passo 1: ServiçoAntifraude → aprovado  ✅
  ├── Passo 2: ServiçoDebito     → debitado  ✅
  ├── Passo 3: ServiçoNotificação → FALHOU   ❌
  │
  └── Compensação:
       └── Passo 2 inverso: ServiçoDebito.CancelarDebitoAsync() ← compensação
```

### Vantagens da Orchestration

- **Fluxo centralizado**: toda a lógica de coordenação está no orquestrador
- **Rastreável**: o estado da Saga é visível em um único lugar (`SagaState`)
- **Fácil de debugar**: olhar o orquestrador diz exatamente em que passo está
- **Rollback claro**: o orquestrador sabe quais passos compensar e em que ordem

### Desvantagens da Orchestration

- **God Object potencial**: o orquestrador pode acumular demasiada lógica
- **Mais acoplado**: o orquestrador conhece todos os serviços
- **Ponto central**: o orquestrador pode virar gargalo

---

## 5. Quando usar cada abordagem

| Critério | Choreography | Orchestration |
|----------|-------------|--------------|
| **Número de serviços** | 2-4 serviços | 5+ serviços |
| **Complexidade do fluxo** | Simples, linear | Complexo, com ramificações |
| **Necessidade de visibilidade** | Baixa | Alta — precisa rastrear estado |
| **Equipes independentes** | Melhor — sem coordenação central | Pior — orquestrador é ponto de coordenação |
| **Debugging em produção** | Mais difícil | Mais fácil |

---

## 6. Compensação — o "rollback distribuído"

### Por que não é um rollback real

```csharp
// ❌ IMPOSSÍVEL em microsserviços
ROLLBACK TRANSACTION; // não existe transação distribuída

// ✅ O que existe: operação de compensação
public async Task CancelarDebitoAsync(Guid cobrancaId)
{
    // Cria uma nova operação que INVERTE o efeito
    // Em produção: cria uma transferência de volta
    // O débito original continua existindo no histórico
}
```

A compensação é semântica, não técnica. O histórico de transações mostra:
1. Débito de R$ 500 às 14:00:01
2. Crédito de R$ 500 às 14:00:03 (compensação)

### Idempotência é obrigatória

O orquestrador pode cair e reiniciar. As operações de compensação podem ser
chamadas mais de uma vez. Cada passo deve ser **idempotente**:

```csharp
public async Task CancelarDebitoAsync(Guid cobrancaId)
{
    // Verificar antes de compensar — pode já ter sido compensado
    if (await JaFoiCancelado(cobrancaId))
        return; // idempotente — segunda chamada não tem efeito
    
    await RealizarCancelamento(cobrancaId);
}
```

---

## 7. Implementação neste módulo

```
Choreography/
  ServicoAntifraudeChoreography  → ouve CobrancaCriada, publica AntifraudeAprovado/Rejeitado
  ServicoDebitoChoreography      → ouve AntifraudeAprovado, publica DebitoRealizado
                                    ouve NotificacaoFalhou, executa compensação
  ServicoNotificacaoChoreography → ouve DebitoRealizado, publica NotificacaoEnviada/Falhou

Orchestration/
  SagaState       → estado completo: qual passo está em que status
  SagaOrchestrator → coordena os 3 passos + compensação
```

O `IEventBus` é uma abstração — em produção seria Apache Kafka ou RabbitMQ.
A implementação `EventBusMemoria` usa `Dictionary<string, Queue<Evento>>` para testes.

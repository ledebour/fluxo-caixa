# ADR-004: Limitação Atual — Sem Outbox Pattern (Evolução Futura)

**Data:** 2025-01  
**Status:** Pendente de implementação

## Contexto

Na implementação atual, ao criar um lançamento o serviço:
1. Persiste no PostgreSQL ✅
2. Publica no RabbitMQ ⚠️

Se o RabbitMQ estiver indisponível no momento da publicação, o evento é **perdido** — o lançamento foi salvo mas o Consolidado nunca será notificado.

## Problema

```
POST /lancamentos
  → BEGIN TRANSACTION
  → INSERT INTO lancamentos ✅
  → COMMIT ✅
  → rabbit.publish() ← FALHA AQUI → evento perdido
```

O Consolidado ficará com saldo desatualizado indefinidamente.

## Solução Futura: Outbox Pattern

```
POST /lancamentos
  → BEGIN TRANSACTION
  → INSERT INTO lancamentos ✅
  → INSERT INTO outbox_events (payload, status='PENDENTE') ✅
  → COMMIT ✅  ← atomicidade garantida

Worker separado:
  → SELECT * FROM outbox_events WHERE status='PENDENTE'
  → rabbit.publish() ✅
  → UPDATE outbox_events SET status='ENVIADO' ✅
```

## Por que não foi implementado agora

O desafio tem escopo de tempo limitado. O comportamento atual é documentado explicitamente no código (comentário `DECISÃO ARQUITETURAL` no publisher) e o sistema degrada de forma segura — o lançamento não é perdido, apenas o evento de notificação.

## Impacto atual

- Lançamentos: **nenhum** (sempre persistem)
- Consolidado: pode ficar desatualizado se RabbitMQ cair durante uma publicação
- Recuperação manual: reprocessar os lançamentos sem evento correspondente

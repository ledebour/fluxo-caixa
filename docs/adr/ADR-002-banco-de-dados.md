# ADR-002: PostgreSQL para Lançamentos e Redis para Cache do Consolidado

**Data:** 2025-01  
**Status:** Aceito

## Contexto

Lançamentos financeiros exigem consistência ACID. O consolidado diário é lido com alta frequência (50 req/s em picos) e pode ser recalculado a partir dos lançamentos.

## Decisão

- **PostgreSQL** para persistência dos lançamentos (ACID, confiável, open-source)
- **Redis** como cache do consolidado diário (latência < 1ms, suporta alta concorrência)

## Consequências

**Positivas:**
- PostgreSQL garante integridade dos dados financeiros
- Redis reduz drasticamente a carga no banco em picos
- TTL no Redis força recálculo periódico, mantendo dados frescos

**Negativas:**
- Cache pode servir dados levemente desatualizados (eventual consistency)
- Necessita estratégia de invalidação de cache ao criar lançamentos

## Estratégia de Cache

```
POST /lancamentos
  → Persiste no PostgreSQL
  → Publica evento no RabbitMQ
  → Consolidado consome evento → invalida cache Redis → recalcula
```

# ADR-003: Cache-Aside com Redis para o Consolidado Diário

**Data:** 2025-01  
**Status:** Aceito

## Contexto

O endpoint de consolidado diário pode receber até 50 req/s em dias de pico, com tolerância máxima de 5% de perda. Uma query PostgreSQL por chamada não sustentaria essa carga sem escalabilidade horizontal cara.

## Decisão

Implementar o padrão **Cache-Aside** com Redis:

1. **Leitura**: verifica Redis primeiro → se HIT retorna imediatamente; se MISS busca no banco e popula o cache
2. **Escrita**: ao processar evento RabbitMQ → salva no banco → invalida a chave Redis
3. **TTL**: 5 minutos — força recálculo periódico mesmo sem eventos

## Consequências

**Positivas:**
- Redis suporta 100k+ req/s com latência < 1ms
- Isola o PostgreSQL de picos de leitura
- Resposta inclui campo `veioDoCache: true/false` para observabilidade

**Negativas:**
- Eventual consistency: saldo pode ter até 5min de delay após um lançamento
- Falha no Redis faz o sistema degradar para leitura direta no banco (fallback implementado)

## Garantia dos 5% de perda

- RabbitMQ com ACK manual: mensagens não se perdem se o Consolidado reiniciar
- Redis com fallback: se o cache cair, o banco responde (com maior latência)
- Em produção: Redis Cluster com replicação elimina o SPOF

## Alternativas Consideradas

| Alternativa | Motivo da Rejeição |
|-------------|-------------------|
| PostgreSQL read replica | Latência ainda ~10ms; operacional mais complexo |
| Cache em memória (in-process) | Não funciona com múltiplas instâncias |
| Materialized View PostgreSQL | Refresh assíncrono cria problemas similares; menos flexível |

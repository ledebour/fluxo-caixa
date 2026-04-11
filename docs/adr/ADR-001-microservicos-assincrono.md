# ADR-001: Arquitetura de Microserviços com Comunicação Assíncrona

**Data:** 2025-01  
**Status:** Aceito

## Contexto

O sistema precisa garantir que o serviço de lançamentos continue operando mesmo que o serviço de consolidado diário fique indisponível. Além disso, o consolidado pode receber picos de até 50 req/s.

## Decisão

Adotar arquitetura de **microserviços desacoplados** com comunicação assíncrona via **RabbitMQ**.

- `FluxoCaixa.Lancamentos.API` — responsável por receber e persistir lançamentos
- `FluxoCaixa.Consolidado.API` — responsável por calcular e servir o saldo consolidado
- Ao criar um lançamento, o evento `LancamentoCriadoEvent` é publicado no RabbitMQ
- O Consolidado consome o evento e atualiza o saldo no Redis

## Consequências

**Positivas:**
- Falha no Consolidado não impacta Lançamentos
- Escalabilidade independente por serviço
- RabbitMQ age como buffer em picos de carga

**Negativas:**
- Eventual consistency (saldo pode ter pequeno delay)
- Maior complexidade operacional
- Necessita infraestrutura adicional (RabbitMQ, Redis)

## Alternativas Consideradas

| Alternativa | Motivo da Rejeição |
|-------------|-------------------|
| Monolito | Violaria o requisito de isolamento de falhas |
| Chamada HTTP síncrona | Acoplamento — se Consolidado cair, Lançamentos falha |
| Serverless | Overhead desnecessário para este contexto |

# 💰 FluxoCaixa — Controle de Fluxo de Caixa

Sistema de controle de fluxo de caixa diário com lançamentos financeiros (débitos/créditos) e consolidado diário. Desenvolvido como desafio de Arquitetura de Software.

---

## 🏗️ Arquitetura

```
┌──────────────────────────────────────────────────────────────┐
│                    Frontend Angular 17                        │
│                   http://localhost:4200                       │
│          ┌──────────────┐    ┌──────────────────┐            │
│          │  Lançamentos │    │    Consolidado    │            │
│          │  (CRUD)      │    │    (Dashboard)    │            │
│          └──────────────┘    └──────────────────┘            │
└────────────────┬──────────────────────┬──────────────────────┘
                 │ REST                 │ REST
    ┌────────────▼──────────┐  ┌────────▼──────────────┐
    │  Lançamentos API      │  │  Consolidado API       │
    │  .NET 8 — porta 5101  │  │  .NET 8 — porta 5102  │
    │  Swagger em /         │  │  Swagger em /          │
    └────────────┬──────────┘  └────────┬──────────────┘
                 │ Publica              │ Consome
                 │    ┌────────────────▼──────┐
                 └───►│  RabbitMQ (topic)      │
                      │  exchange: fluxo-caixa │
                      │  queue: consolidado    │
                      └────────────────────────┘
    ┌──────────────────────┐  ┌────────────────────────┐
    │  PostgreSQL 16        │  │  Redis 7               │
    │  tabela: lancamentos  │  │  cache: consolidado    │
    │  porta 5432           │  │  TTL: 5min — porta 6379│
    └──────────────────────┘  └────────────────────────┘
```

### Decisões Arquiteturais

| Decisão | Justificativa |
|---------|--------------|
| **Microserviços** | Lançamentos continuam operando mesmo se Consolidado cair |
| **RabbitMQ (topic)** | Comunicação assíncrona — desacopla os serviços |
| **Redis Cache-Aside** | Suporta 50 req/s com < 5% perda — latência < 1ms |
| **ACK manual RabbitMQ** | Eventos não se perdem se o Consolidado reiniciar |
| **PostgreSQL** | ACID para dados financeiros — consistência crítica |
| **Angular 17 Standalone** | Signals + lazy loading + zero dependências de UI |

> Ver decisões completas em [`/docs/adr`](./docs/adr/)

---

## 🗂️ Estrutura do Repositório

```
fluxo-caixa/
├── src/
│   ├── FluxoCaixa.Lancamentos.API/      # Microserviço de Lançamentos
│   │   ├── Domain/                      # Entidades, interfaces, exceções
│   │   ├── Application/                 # Use Cases, DTOs
│   │   ├── Infrastructure/              # EF Core, RabbitMQ publisher, repo
│   │   └── API/                         # Controllers, Middleware
│   ├── FluxoCaixa.Consolidado.API/      # Microserviço de Consolidado
│   │   ├── Domain/
│   │   ├── Application/                 # Use Cases com Cache-Aside
│   │   └── Infrastructure/              # Redis cache, RabbitMQ consumer
│   ├── FluxoCaixa.Shared/               # Eventos e contratos compartilhados
│   ├── FluxoCaixa.Lancamentos.API.Tests/ # Testes unitários (xUnit + NSubstitute)
│   └── FluxoCaixa.Consolidado.API.Tests/
├── frontend/                            # Angular 17
│   └── src/app/
│       ├── core/                        # Services, models, interceptors
│       └── features/                    # Lançamentos + Consolidado
├── infra/
│   ├── docker-compose.yml              # Infraestrutura completa
│   └── init-db.sql
├── docs/adr/                           # Decisões arquiteturais documentadas
├── start-dev.sh                        # Script para subir tudo localmente
└── FluxoCaixa.sln
```

---

## 🚀 Como Rodar Localmente

### Opção A — Script automático (recomendado)

```bash
# Clone o repositório
git clone https://github.com/ledebour/fluxo-caixa.git
cd fluxo-caixa

# Sobe tudo com um comando
chmod +x start-dev.sh
./start-dev.sh
```

### Opção B — Passo a passo manual

#### Pré-requisitos
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) + Angular CLI: `npm i -g @angular/cli@17`

#### 1. Infraestrutura

```bash
cd infra
docker compose up -d
# PostgreSQL:5432 | Redis:6379 | RabbitMQ:5672 (Manager: 15672)
```

#### 2. Serviço de Lançamentos

```bash
cd src/FluxoCaixa.Lancamentos.API
dotnet run
# API: http://localhost:5101
# Swagger: http://localhost:5001
```

#### 3. Serviço de Consolidado

```bash
cd src/FluxoCaixa.Consolidado.API
dotnet run
# API: http://localhost:5102
# Swagger: http://localhost:5002
```

#### 4. Frontend

```bash
cd frontend
npm install
ng serve
# App: http://localhost:4200
```

### Opção C — Docker Compose completo

```bash
cd infra
docker compose --profile full up -d
# Sobe tudo incluindo os microserviços e frontend
```

---

## 🌐 URLs e Acessos

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| Frontend | http://localhost:4200 | — |
| Lançamentos API (Swagger) | http://localhost:5101 | — |
| Consolidado API (Swagger) | http://localhost:5102 | — |
| RabbitMQ Management | http://localhost:15672 | fluxo / fluxo123 |
| PostgreSQL | localhost:5432 | fluxo / fluxo123 |
| Redis | localhost:6379 | — |

---

## 📡 Endpoints

### Lançamentos API (`localhost:5101`)

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/lancamentos` | Lista todos os lançamentos |
| `GET` | `/api/lancamentos/{id}` | Busca por ID |
| `GET` | `/api/lancamentos/por-data/{data}` | Filtra por data (yyyy-MM-dd) |
| `POST` | `/api/lancamentos` | Cria lançamento (débito ou crédito) |
| `DELETE` | `/api/lancamentos/{id}` | Remove lançamento |
| `GET` | `/api/health` | Health check |

**Exemplo de criação:**
```json
POST /api/lancamentos
{
  "data": "2025-01-15",
  "valor": 1500.00,
  "tipo": "Credito",
  "descricao": "Pagamento de cliente"
}
```

### Consolidado API (`localhost:5102`)

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/consolidado/{data}` | Saldo de uma data (yyyy-MM-dd) |
| `GET` | `/api/consolidado/periodo?inicio=&fim=` | Consolidado de um período |

---

## 🧪 Testes

```bash
# Todos os testes
dotnet test

# Por projeto
dotnet test src/FluxoCaixa.Lancamentos.API.Tests
dotnet test src/FluxoCaixa.Consolidado.API.Tests

# Com cobertura
dotnet test --collect:"XPlat Code Coverage"
```

**Cobertura atual:**
- Domínio de Lançamentos: 13 testes unitários
- Use Cases de Lançamentos: 3 testes com mocks (NSubstitute)
- Domínio de Consolidado: 8 testes unitários
- Use Cases de Consolidado: 3 testes de integração (fluxo RabbitMQ → consolidado)

---

## 📋 Requisitos Não-Funcionais Atendidos

### ✅ Resiliência
- Lançamentos operam **independentemente** do Consolidado
- Falha no RabbitMQ: lançamento é salvo; evento é logado como perdido (sem rollback)
- Falha no Redis: fallback automático para PostgreSQL
- Consumer RabbitMQ com **NACK + requeue**: retry automático em falhas

### ✅ Escalabilidade
- Consolidado suporta **50 req/s** via cache Redis (< 1ms de latência)
- RabbitMQ com `prefetchCount=1` evita sobrecarga do consumer
- Serviços escalam horizontalmente de forma independente

### ✅ Performance
- `AsNoTracking()` em todas as queries de leitura
- Índices no PostgreSQL: `(data)`, `(tipo)`, `(data, tipo)`
- TTL de 5 minutos no Redis — balanceia frescor vs performance

### ✅ Segurança
- CORS configurado explicitamente por origem
- Usuário não-root nos containers Docker
- Stack trace exposto apenas em `Development`
- Erros HTTP retornam mensagens padronizadas sem vazar internos

---

## 📐 Padrões e Boas Práticas

- **DDD** — entidades com invariantes protegidas, factory methods, sem setters públicos
- **Clean Architecture** — Domain → Application → Infrastructure (dependências unidirecionais)
- **SOLID** — DIP via interfaces, SRP por Use Case, OCP no middleware de exceções
- **Cache-Aside** — Redis como cache de leitura com invalidação por evento
- **Outbox Pattern** — documentado como evolução futura (ADR-004)

---

## 🔮 Evoluções Futuras

- [ ] **Outbox Pattern** — garantia total de entrega de eventos (ADR-004)
- [ ] **API Gateway** (YARP/Ocelot) — ponto único de entrada, rate limiting
- [ ] **Autenticação JWT** — Keycloak ou ASP.NET Identity
- [ ] **Observabilidade** — OpenTelemetry + Grafana + Jaeger (tracing distribuído)
- [ ] **Kubernetes** — Helm charts, HPA para auto-scaling
- [ ] **Redis Cluster** — eliminar SPOF do cache
- [ ] **Event Sourcing** — auditoria completa de todos os lançamentos
- [ ] **Dead Letter Queue** — fila para mensagens que falharam repetidamente

# 💰 Fluxo de Caixa — Arquitetura de Microserviços

Sistema de controle de fluxo de caixa diário com lançamentos (débitos/créditos) e consolidado diário.

---

## 📐 Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                        Frontend Angular                      │
│              (Lançamentos | Consolidado Diário)              │
└───────────────────┬─────────────────────┬────────────────────┘
                    │ HTTP/REST            │ HTTP/REST
          ┌─────────▼──────────┐ ┌────────▼───────────┐
          │  Lançamentos API   │ │  Consolidado API   │
          │   (.NET Core)      │ │   (.NET Core)      │
          └─────────┬──────────┘ └────────┬───────────┘
                    │ Publica              │ Consome
                    │     ┌───────────────▼──────┐
                    └────►│      RabbitMQ         │
                          │   (Mensageria)        │
                          └───────────────────────┘
          ┌──────────────────┐  ┌────────────────────┐
          │   PostgreSQL     │  │       Redis         │
          │  (Lançamentos)   │  │  (Cache Consolidado)│
          └──────────────────┘  └────────────────────┘
```

### Por que essa arquitetura?

| Decisão | Justificativa |
|---------|--------------|
| **Microserviços** | Isolamento de falhas — se o Consolidado cair, Lançamentos continua operando |
| **RabbitMQ** | Comunicação assíncrona garante desacoplamento entre serviços |
| **Redis** | Cache do consolidado diário suporta 50 req/s com mínima perda |
| **PostgreSQL** | ACID para lançamentos financeiros — consistência é crítica |

---

## 🗂️ Estrutura do Repositório

```
fluxo-caixa/
├── src/
│   ├── FluxoCaixa.Lancamentos.API/     # Microserviço de Lançamentos
│   ├── FluxoCaixa.Consolidado.API/     # Microserviço de Consolidado
│   └── FluxoCaixa.Shared/              # Contratos e eventos compartilhados
├── frontend/                           # Aplicação Angular
├── infra/                              # Docker Compose e configurações
├── docs/                               # Diagramas e documentação
└── README.md
```

---

## 🚀 Como Rodar Localmente

### Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Angular CLI](https://angular.io/cli): `npm install -g @angular/cli`

### 1. Subir infraestrutura (PostgreSQL, Redis, RabbitMQ)

```bash
cd infra
docker-compose up -d
```

### 2. Rodar o serviço de Lançamentos

```bash
cd src/FluxoCaixa.Lancamentos.API
dotnet run
# API disponível em: http://localhost:5001
```

### 3. Rodar o serviço de Consolidado

```bash
cd src/FluxoCaixa.Consolidado.API
dotnet run
# API disponível em: http://localhost:5002
```

### 4. Rodar o Frontend

```bash
cd frontend
npm install
ng serve
# App disponível em: http://localhost:4200
```

---

## 📡 Endpoints Principais

### Lançamentos API (`localhost:5001`)

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/lancamentos` | Lista todos os lançamentos |
| `POST` | `/api/lancamentos` | Cria novo lançamento |
| `GET` | `/api/lancamentos/{id}` | Busca lançamento por ID |
| `DELETE` | `/api/lancamentos/{id}` | Remove lançamento |

### Consolidado API (`localhost:5002`)

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/consolidado/{data}` | Saldo consolidado de uma data |
| `GET` | `/api/consolidado/periodo` | Consolidado por período |

---

## 🧪 Testes

```bash
# Rodar todos os testes
dotnet test

# Testes de um projeto específico
dotnet test src/FluxoCaixa.Lancamentos.API.Tests
```

---

## 📊 Requisitos Não-Funcionais Atendidos

- ✅ **Resiliência**: Lançamentos operam independentemente do Consolidado
- ✅ **Escalabilidade**: RabbitMQ absorve picos; Redis caches consolidado
- ✅ **Performance**: Consolidado suporta 50 req/s via cache Redis
- ✅ **Segurança**: Autenticação JWT, HTTPS, validação de entrada

---

## 📌 Evoluções Futuras

- [ ] API Gateway (YARP ou Ocelot)
- [ ] Autenticação via Keycloak/OAuth2
- [ ] Observabilidade com OpenTelemetry + Grafana
- [ ] Deploy em Kubernetes (Helm charts)
- [ ] Event Sourcing para auditoria completa

---

## 🏗️ Decisões Arquiteturais (ADRs)

Documentadas em [`/docs/adr`](./docs/adr/).

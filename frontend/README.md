# FluxoCaixa — Frontend Angular

Interface web para o sistema de controle de fluxo de caixa.

## Stack

- **Angular 17** (standalone components, signals)
- **SCSS** (design system próprio, sem dependências de UI)
- **Lazy loading** por rota

## Pré-requisitos

```bash
node -v  # >= 20
npm install -g @angular/cli@17
```

## Rodar localmente

```bash
npm install
ng serve          # http://localhost:4200
```

O proxy redireciona automaticamente:
- `/api/lancamentos` → `http://localhost:5001`
- `/api/consolidado` → `http://localhost:5002`

## Build de produção

```bash
ng build --configuration production
```

## Estrutura

```
src/app/
├── core/
│   ├── models/models.ts          ← interfaces TypeScript
│   ├── services/
│   │   ├── lancamentos.service.ts
│   │   └── consolidado.service.ts
│   └── interceptors/api.interceptor.ts
├── features/
│   ├── lancamentos/pages/        ← CRUD de lançamentos
│   └── consolidado/pages/        ← dashboard de saldo
└── app.routes.ts                 ← lazy loading por rota
```

## Páginas

| Rota | Descrição |
|------|-----------|
| `/lancamentos` | Lista, cria e remove débitos/créditos |
| `/consolidado` | Saldo diário e histórico por período com gráfico |

-- ─── Schema e extensões ───────────────────────────────────────────────────────
CREATE SCHEMA IF NOT EXISTS public;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
SET timezone = 'UTC';

-- ─── Lançamentos ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS public.lancamentos (
    id          UUID            PRIMARY KEY DEFAULT uuid_generate_v4(),
    data        DATE            NOT NULL,
    descricao   VARCHAR(255)    NOT NULL,
    tipo        VARCHAR(50)     NOT NULL,
    valor       NUMERIC(18,2)   NOT NULL,
    criado_em   TIMESTAMP       NOT NULL DEFAULT NOW()
);

-- ─── Consolidado Diário ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS public.consolidados_diarios (
    id                      UUID            PRIMARY KEY DEFAULT uuid_generate_v4(),
    data                    DATE            NOT NULL UNIQUE,
    total_creditos          NUMERIC(18,2)   NOT NULL DEFAULT 0,
    total_debitos           NUMERIC(18,2)   NOT NULL DEFAULT 0,
    quantidade_lancamentos  INT             NOT NULL DEFAULT 0,
    atualizado_em           TIMESTAMP       NOT NULL DEFAULT NOW()
);

-- ─── Índices ──────────────────────────────────────────────────────────────────
CREATE INDEX IF NOT EXISTS idx_lancamentos_data      ON public.lancamentos (data DESC);
CREATE INDEX IF NOT EXISTS idx_consolidados_data     ON public.consolidados_diarios (data DESC);
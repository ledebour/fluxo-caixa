-- Script de inicialização do PostgreSQL
-- As migrations do EF Core criam as tabelas automaticamente.
-- Este script garante que o schema público existe e habilita extensões úteis.

CREATE SCHEMA IF NOT EXISTS public;

-- Extensão para geração de UUIDs (utilizada pelo EF Core)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Configurações de timezone
SET timezone = 'UTC';

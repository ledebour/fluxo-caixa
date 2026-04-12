#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# start-dev.sh — Sobe a infraestrutura e os serviços para desenvolvimento local
# Uso: ./start-dev.sh
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

log()  { echo -e "${GREEN}[✓]${NC} $1"; }
info() { echo -e "${CYAN}[→]${NC} $1"; }
warn() { echo -e "${YELLOW}[!]${NC} $1"; }

echo ""
echo "  F  FluxoCaixa — Ambiente de Desenvolvimento"
echo "  ─────────────────────────────────────────────"
echo ""

# 1. Infraestrutura Docker
info "Subindo infraestrutura (PostgreSQL, Redis, RabbitMQ)..."
cd infra
docker compose up -d
cd ..

# 2. Aguarda o PostgreSQL estar pronto
info "Aguardando PostgreSQL ficar saudável..."
until docker exec fluxo-postgres pg_isready -U fluxo -d fluxo_caixa &>/dev/null; do
  sleep 1
done
log "PostgreSQL pronto"

# 3. Aguarda o RabbitMQ
info "Aguardando RabbitMQ ficar saudável..."
sleep 5
log "RabbitMQ pronto"

# 4. Serviço de Lançamentos
info "Iniciando Lançamentos API (porta 5101)..."
cd src/FluxoCaixa.Lancamentos.API
dotnet run --no-launch-profile \
  --urls "http://localhost:5001" \
  --environment Development &
LANCAMENTOS_PID=$!
cd ../..

sleep 3

# 5. Serviço de Consolidado
info "Iniciando Consolidado API (porta 5102)..."
cd src/FluxoCaixa.Consolidado.API
dotnet run --no-launch-profile \
  --urls "http://localhost:5002" \
  --environment Development &
CONSOLIDADO_PID=$!
cd ../..

sleep 3

# 6. Frontend Angular
info "Iniciando Frontend Angular (porta 4200)..."
cd frontend
npm install --silent
ng serve &
FRONTEND_PID=$!
cd ..

echo ""
log "Todos os serviços iniciados!"
echo ""
echo "  🌐  Frontend:           http://localhost:4200"
echo "  ⚙️   Lançamentos API:    http://localhost:5101
echo "  ⚙️   Lançamentos API Swagger:    http://localhost:5001
echo "  📊  Consolidado API:    http://localhost:5102
echo "  📊  Consolidado API Swagger:    http://localhost:5002
echo "  🐰  RabbitMQ Manager:   http://localhost:15672  (fluxo/fluxo123)"
echo ""
echo "  Pressione Ctrl+C para encerrar todos os serviços"
echo ""

# Encerra tudo no Ctrl+C
trap "kill $LANCAMENTOS_PID $CONSOLIDADO_PID $FRONTEND_PID 2>/dev/null; docker compose -f infra/docker-compose.yml stop; echo ''; warn 'Serviços encerrados.'" EXIT

wait

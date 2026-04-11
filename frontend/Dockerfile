# ── Stage 1: Build Angular ────────────────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

# Copia e instala dependências (layer de cache)
COPY package*.json ./
RUN npm ci --silent

# Copia o restante e faz build de produção
COPY . .
RUN npm run build:prod

# ── Stage 2: Nginx ────────────────────────────────────────────────────────────
FROM nginx:alpine AS runtime

# Remove config padrão do nginx
RUN rm /etc/nginx/conf.d/default.conf

# Config customizada com proxy para os backends
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copia os arquivos buildados
COPY --from=build /app/dist/fluxo-caixa-frontend/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

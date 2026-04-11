# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copia os .csproj e restaura dependências (layer de cache)
COPY FluxoCaixa.Lancamentos.API.csproj ./FluxoCaixa.Lancamentos.API/
COPY ../FluxoCaixa.Shared/FluxoCaixa.Shared.csproj ./FluxoCaixa.Shared/
RUN dotnet restore ./FluxoCaixa.Lancamentos.API/FluxoCaixa.Lancamentos.API.csproj

# Copia o restante do código e publica
COPY . ./FluxoCaixa.Lancamentos.API/
COPY ../FluxoCaixa.Shared/ ./FluxoCaixa.Shared/

RUN dotnet publish ./FluxoCaixa.Lancamentos.API/FluxoCaixa.Lancamentos.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Usuário não-root para segurança
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

# Porta interna do container
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "FluxoCaixa.Lancamentos.API.dll"]

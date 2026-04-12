# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Restaurar dependências (aproveita cache do Docker)
COPY FluxoCaixa.Shared/FluxoCaixa.Shared.csproj                          ./FluxoCaixa.Shared/
COPY FluxoCaixa.Consolidado.API/FluxoCaixa.Consolidado.API.csproj         ./FluxoCaixa.Consolidado.API/
RUN dotnet restore ./FluxoCaixa.Consolidado.API/FluxoCaixa.Consolidado.API.csproj

# Copiar código-fonte
COPY FluxoCaixa.Shared/           ./FluxoCaixa.Shared/
COPY FluxoCaixa.Consolidado.API/  ./FluxoCaixa.Consolidado.API/

RUN dotnet publish ./FluxoCaixa.Consolidado.API/FluxoCaixa.Consolidado.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "FluxoCaixa.Consolidado.API.dll"]
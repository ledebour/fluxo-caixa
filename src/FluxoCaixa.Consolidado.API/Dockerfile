# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY FluxoCaixa.Consolidado.API.csproj ./FluxoCaixa.Consolidado.API/
COPY ../FluxoCaixa.Shared/FluxoCaixa.Shared.csproj ./FluxoCaixa.Shared/
RUN dotnet restore ./FluxoCaixa.Consolidado.API/FluxoCaixa.Consolidado.API.csproj

COPY . ./FluxoCaixa.Consolidado.API/
COPY ../FluxoCaixa.Shared/ ./FluxoCaixa.Shared/

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

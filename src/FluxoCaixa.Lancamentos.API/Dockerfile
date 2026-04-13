# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY FluxoCaixa.Shared/FluxoCaixa.Shared.csproj                          ./FluxoCaixa.Shared/
COPY FluxoCaixa.Lancamentos.API/FluxoCaixa.Lancamentos.API.csproj         ./FluxoCaixa.Lancamentos.API/
RUN dotnet restore ./FluxoCaixa.Lancamentos.API/FluxoCaixa.Lancamentos.API.csproj

COPY FluxoCaixa.Shared/          ./FluxoCaixa.Shared/
COPY FluxoCaixa.Lancamentos.API/ ./FluxoCaixa.Lancamentos.API/

RUN dotnet publish ./FluxoCaixa.Lancamentos.API/FluxoCaixa.Lancamentos.API.csproj \
    -c Release \
    -o /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "FluxoCaixa.Lancamentos.API.dll"]
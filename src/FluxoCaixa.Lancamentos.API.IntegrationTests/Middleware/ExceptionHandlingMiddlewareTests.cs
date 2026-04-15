using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluxoCaixa.Lancamentos.API.Application.DTOs;
using FluxoCaixa.Lancamentos.API.Application.UseCases;
using FluxoCaixa.Lancamentos.API.Domain.Exceptions;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using FluxoCaixa.Lancamentos.API.Infrastructure.Data;

namespace FluxoCaixa.Lancamentos.API.IntegrationTests.Middleware;

/// <summary>
/// Testa o ExceptionHandlingMiddleware e HealthController.
/// Injeta repositório com falhas forçadas para validar mapeamento de exceções → HTTP status.
/// </summary>
public class ExceptionHandlingMiddlewareTests : IClassFixture<LancamentosApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExceptionHandlingMiddlewareTests(LancamentosApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── HealthController ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetHealth_DeveRetornar200()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_DeveRetornarStatusHealthy()
    {
        var response = await _client.GetAsync("/api/health");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", body);
    }

    [Fact]
    public async Task GetHealth_DeveRetornarNomeServico()
    {
        var response = await _client.GetAsync("/api/health");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("FluxoCaixa.Lancamentos.API", body);
    }

    // ─── Middleware — NotFoundException → 404 ─────────────────────────────────

    [Fact]
    public async Task GetPorId_IdInexistente_DeveRetornar404ViaMiddleware()
    {
        var response = await _client.GetAsync($"/api/lancamentos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("não encontrado", body!.Mensagem, StringComparison.OrdinalIgnoreCase);
    }

    // ─── Middleware — DomainException → 422 ──────────────────────────────────

    [Fact]
    public async Task Post_ComDadosDomainInvalidos_DeveRetornar422ViaMiddleware()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today.AddDays(1),
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Data futura"
        };
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await _client.PostAsync("/api/lancamentos",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.Mensagem));
    }

    // ─── Middleware — resposta em JSON com timestamp ───────────────────────────

    [Fact]
    public async Task QuandoErroOcorre_ResponseDeveConterTimestamp()
    {
        var response = await _client.GetAsync($"/api/lancamentos/{Guid.NewGuid()}");

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(default, body!.Timestamp);
    }

    [Fact]
    public async Task QuandoErroOcorre_ContentTypeDeveSerJson()
    {
        var response = await _client.GetAsync($"/api/lancamentos/{Guid.NewGuid()}");

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}

/// <summary>
/// Testa o middleware com erros genéricos (500) via factory com repositório falhando.
/// </summary>
public class ExceptionMiddlewareInternalErrorTests
{
    [Fact]
    public async Task QuandoRepositorioLancaExcecaoGenerica_DeveRetornar500()
    {
        var repo = Substitute.For<ILancamentoRepository>();
        repo.ObterTodosAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Erro inesperado"));

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    var dbDesc = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<LancamentosDbContext>));
                    if (dbDesc != null) services.Remove(dbDesc);
                    services.AddDbContext<LancamentosDbContext>(o =>
                        o.UseInMemoryDatabase($"err_{Guid.NewGuid()}"));

                    var repoDesc = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ILancamentoRepository));
                    if (repoDesc != null) services.Remove(repoDesc);
                    services.AddScoped(_ => repo);

                    var pubDesc = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IEventPublisher));
                    if (pubDesc != null) services.Remove(pubDesc);
                    services.AddSingleton(Substitute.For<IEventPublisher>());
                });
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/lancamentos");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}

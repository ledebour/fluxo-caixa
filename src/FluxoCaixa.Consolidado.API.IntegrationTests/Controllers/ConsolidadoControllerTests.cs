using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluxoCaixa.Consolidado.API.Application.DTOs;
using FluxoCaixa.Consolidado.API.Domain.Entities;
using FluxoCaixa.Consolidado.API.Domain.Interfaces;
using FluxoCaixa.Consolidado.API.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;
using Xunit;

namespace FluxoCaixa.Consolidado.API.IntegrationTests.Controllers;

/// <summary>
/// Testes de integração para ConsolidadoController.
/// Cobre: 200, 404, 400, serialização, middleware de exceção.
/// </summary>
public class ConsolidadoControllerTests : IClassFixture<ConsolidadoApiFactory>
{
    private readonly HttpClient _client;
    private readonly ConsolidadoApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConsolidadoControllerTests(ConsolidadoApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ─── GET /api/consolidado/{data} ──────────────────────────────────────────

    [Fact]
    public async Task GetPorData_QuandoNaoExiste_DeveRetornar404()
    {
        _factory.CacheSubstitute.ObterAsync(Arg.Any<DateTime>())
            .Returns((ConsolidadoDiario?)null);

        var response = await _client.GetAsync("/api/consolidado/2020-01-01");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPorData_QuandoNaoExiste_DeveRetornarErrorResponse()
    {
        _factory.CacheSubstitute.ObterAsync(Arg.Any<DateTime>())
            .Returns((ConsolidadoDiario?)null);

        var response = await _client.GetAsync("/api/consolidado/2020-01-02");

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("2020-01-02", body!.Mensagem);
    }

    [Fact]
    public async Task GetPorData_QuandoCacheRetornaDados_DeveRetornar200()
    {
        var data = new DateTime(2025, 6, 15);
        var consolidado = ConsolidadoDiario.Criar(data);
        consolidado.AplicarCredito(1000m);
        _factory.CacheSubstitute.ObterAsync(data.Date, Arg.Any<CancellationToken>()).Returns(consolidado);

        var response = await _client.GetAsync($"/api/consolidado/{data:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPorData_QuandoCacheRetornaDados_DeveRetornarResponseCorreto()
    {
        var data = new DateTime(2025, 7, 10);
        var consolidado = ConsolidadoDiario.Criar(data);
        consolidado.AplicarCredito(500m);
        consolidado.AplicarDebito(200m);
        _factory.CacheSubstitute.ObterAsync(data.Date, Arg.Any<CancellationToken>()).Returns(consolidado);

        var response = await _client.GetAsync($"/api/consolidado/{data:yyyy-MM-dd}");
        var body = await response.Content.ReadFromJsonAsync<ConsolidadoDiarioResponse>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(500m, body!.TotalCreditos);
        Assert.Equal(200m, body.TotalDebitos);
        Assert.Equal(300m, body.SaldoFinal);
        Assert.True(body.VeioDoCache);
    }

    [Fact]
    public async Task GetPorData_DeveRetornarContentTypeJson()
    {
        var data = new DateTime(2025, 8, 1);
        var consolidado = ConsolidadoDiario.Criar(data);
        _factory.CacheSubstitute.ObterAsync(data.Date).Returns(consolidado);

        var response = await _client.GetAsync($"/api/consolidado/{data:yyyy-MM-dd}");

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    // ─── GET /api/consolidado/periodo ─────────────────────────────────────────

    [Fact]
    public async Task GetPorPeriodo_ComDatasValidas_DeveRetornar200()
    {
        _factory.CacheSubstitute.ObterAsync(Arg.Any<DateTime>())
            .Returns((ConsolidadoDiario?)null);

        var response = await _client.GetAsync(
            "/api/consolidado/periodo?inicio=2025-01-01&fim=2025-01-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPorPeriodo_ComDatasValidas_DeveRetornarPeriodoResponse()
    {
        _factory.CacheSubstitute.ObterAsync(Arg.Any<DateTime>())
            .Returns((ConsolidadoDiario?)null);

        var response = await _client.GetAsync(
            "/api/consolidado/periodo?inicio=2025-02-01&fim=2025-02-28");
        var body = await response.Content.ReadFromJsonAsync<ConsolidadoPeriodoResponse>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(new DateTime(2025, 2, 1), body!.DataInicio.Date);
        Assert.Equal(new DateTime(2025, 2, 28), body.DataFim.Date);
    }

    [Fact]
    public async Task GetPorPeriodo_ComInicioMaiorQueFim_DeveRetornar400()
    {
        var response = await _client.GetAsync(
            "/api/consolidado/periodo?inicio=2025-12-31&fim=2025-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPorPeriodo_ComInicioMaiorQueFim_DeveRetornarErrorResponse()
    {
        var response = await _client.GetAsync(
            "/api/consolidado/periodo?inicio=2025-12-31&fim=2025-01-01");

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.Mensagem));
    }

    // ─── Middleware — InternalServerError → 500 ───────────────────────────────

    [Fact]
    public async Task QuandoUseCaseLancaExcecaoGenerica_DeveRetornar500()
    {
        var cacheComErro = Substitute.For<IConsolidadoCache>();
        cacheComErro.ObterAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Erro inesperado de infra"));

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    var dbDesc = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ConsolidadoDbContext>));
                    if (dbDesc != null) services.Remove(dbDesc);
                    services.AddDbContext<ConsolidadoDbContext>(o =>
                        o.UseInMemoryDatabase($"err_{Guid.NewGuid()}"));

                    var redisDesc = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IConnectionMultiplexer));
                    if (redisDesc != null) services.Remove(redisDesc);

                    var cacheDesc = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IConsolidadoCache));
                    if (cacheDesc != null) services.Remove(cacheDesc);
                    services.AddSingleton(cacheComErro);

                    var hosted = services
                        .Where(d => d.ImplementationType?.Name == "RabbitMqConsumerService")
                        .ToList();
                    foreach (var s in hosted) services.Remove(s);
                });
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/consolidado/2025-01-01");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task QuandoErroOcorre_ResponseDeveConterTimestamp()
    {
        _factory.CacheSubstitute.ObterAsync(Arg.Any<DateTime>())
            .Returns((ConsolidadoDiario?)null);

        var response = await _client.GetAsync("/api/consolidado/1900-01-01");
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);

        Assert.NotNull(body);
        Assert.NotEqual(default, body!.Timestamp);
    }
}

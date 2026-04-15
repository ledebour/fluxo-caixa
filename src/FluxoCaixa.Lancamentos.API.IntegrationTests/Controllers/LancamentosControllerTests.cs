using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluxoCaixa.Lancamentos.API.Application.DTOs;
using FluxoCaixa.Shared.Enums;
using Xunit;

namespace FluxoCaixa.Lancamentos.API.IntegrationTests.Controllers;

/// <summary>
/// Testes de integração para LancamentosController.
/// Cobre todos os endpoints HTTP, status codes e serialização de resposta.
/// </summary>
public class LancamentosControllerTests : IClassFixture<LancamentosApiFactory>
{
    private readonly HttpClient _client;
    private readonly LancamentosApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LancamentosControllerTests(LancamentosApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ─── GET /api/lancamentos ─────────────────────────────────────────────────

    [Fact]
    public async Task GetTodos_SemLancamentos_DeveRetornar200ComListaVazia()
    {
        var response = await _client.GetAsync("/api/lancamentos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<LancamentoResponse>>(JsonOptions);
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetTodos_ApósCriarLancamento_DeveRetornarLancamento()
    {
        await CriarLancamentoAsync(200m, TipoLancamento.Credito, "Teste listagem");

        var response = await _client.GetAsync("/api/lancamentos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<LancamentoResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains(body!, l => l.Descricao == "Teste listagem");
    }

    [Fact]
    public async Task GetTodos_DeveRetornarContentTypeJson()
    {
        var response = await _client.GetAsync("/api/lancamentos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    // ─── GET /api/lancamentos/{id} ────────────────────────────────────────────

    [Fact]
    public async Task GetPorId_QuandoExiste_DeveRetornar200ComDados()
    {
        var criado = await CriarLancamentoAsync(350m, TipoLancamento.Debito, "Busca por ID");

        var response = await _client.GetAsync($"/api/lancamentos/{criado!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LancamentoResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(criado.Id, body!.Id);
        Assert.Equal(350m, body.Valor);
        Assert.Equal("Debito", body.Tipo);
    }

    [Fact]
    public async Task GetPorId_QuandoNaoExiste_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/lancamentos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPorId_QuandoNaoExiste_DeveRetornarErrorResponse()
    {
        var response = await _client.GetAsync($"/api/lancamentos/{Guid.NewGuid()}");

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.Mensagem));
    }

    // ─── GET /api/lancamentos/por-data/{data} ─────────────────────────────────

    [Fact]
    public async Task GetPorData_DeveRetornar200()
    {
        var response = await _client.GetAsync($"/api/lancamentos/por-data/{DateTime.Today:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPorData_ApósCriar_DeveRetornarLancamentosDaData()
    {
        var data = DateTime.Today;
        await CriarLancamentoAsync(100m, TipoLancamento.Credito, "Por data teste", data);

        var response = await _client.GetAsync($"/api/lancamentos/por-data/{data:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<LancamentoResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains(body!, l => l.Descricao == "Por data teste");
    }

    // ─── POST /api/lancamentos ────────────────────────────────────────────────

    [Fact]
    public async Task Post_ComRequestValido_DeveRetornar201()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 500m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Criação válida"
        };

        var response = await PostLancamentoAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComRequestValido_DeveRetornarLancamentoCriado()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 750m,
            Tipo = TipoLancamento.Debito,
            Descricao = "Retorno criação"
        };

        var response = await PostLancamentoAsync(request);
        var body = await response.Content.ReadFromJsonAsync<LancamentoResponse>(JsonOptions);

        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal(750m, body.Valor);
        Assert.Equal("Debito", body.Tipo);
    }

    [Fact]
    public async Task Post_ComRequestValido_DeveRetornarHeaderLocation()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Header location"
        };

        var response = await PostLancamentoAsync(request);

        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/api/lancamentos/", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_ComDataFutura_DeveRetornar422()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today.AddDays(5),
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Data futura"
        };

        var response = await PostLancamentoAsync(request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComValorZero_DeveRetornar422()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 0m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Valor zero"
        };

        var response = await PostLancamentoAsync(request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComDescricaoVazia_DeveRetornar422()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Descricao = ""
        };

        var response = await PostLancamentoAsync(request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComErro_DeveRetornarErrorResponseComMensagem()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today.AddDays(10),
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Erro"
        };

        var response = await PostLancamentoAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);

        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.Mensagem));
    }

    // ─── DELETE /api/lancamentos/{id} ─────────────────────────────────────────

    [Fact]
    public async Task Delete_QuandoExiste_DeveRetornar204()
    {
        var criado = await CriarLancamentoAsync(150m, TipoLancamento.Credito, "Para deletar");

        var response = await _client.DeleteAsync($"/api/lancamentos/{criado!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_QuandoNaoExiste_DeveRetornar404()
    {
        var response = await _client.DeleteAsync($"/api/lancamentos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ApósDeletar_GetDeveRetornar404()
    {
        var criado = await CriarLancamentoAsync(100m, TipoLancamento.Debito, "Deletar e verificar");

        await _client.DeleteAsync($"/api/lancamentos/{criado!.Id}");
        var getResponse = await _client.GetAsync($"/api/lancamentos/{criado.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<LancamentoResponse?> CriarLancamentoAsync(
        decimal valor, TipoLancamento tipo, string descricao, DateTime? data = null)
    {
        var request = new CriarLancamentoRequest
        {
            Data = data ?? DateTime.Today,
            Valor = valor,
            Tipo = tipo,
            Descricao = descricao
        };
        var response = await PostLancamentoAsync(request);
        return await response.Content.ReadFromJsonAsync<LancamentoResponse>(JsonOptions);
    }

    private Task<HttpResponseMessage> PostLancamentoAsync(CriarLancamentoRequest request)
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return _client.PostAsync("/api/lancamentos", content);
    }
}

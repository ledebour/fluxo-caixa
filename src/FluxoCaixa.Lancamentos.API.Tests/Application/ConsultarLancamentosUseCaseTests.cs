using FluxoCaixa.Lancamentos.API.Application.UseCases;
using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Lancamentos.API.Domain.Exceptions;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Shared.Enums;
using NSubstitute;
using Xunit;
namespace FluxoCaixa.Lancamentos.API.Tests.Application;

public class ConsultarLancamentosUseCaseTests
{
    private readonly ILancamentoRepository _repository = Substitute.For<ILancamentoRepository>();
    private readonly ConsultarLancamentosUseCase _useCase;

    public ConsultarLancamentosUseCaseTests()
    {
        _useCase = new ConsultarLancamentosUseCase(_repository);
    }

    // ─── ObterTodosAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ObterTodosAsync_QuandoExistemLancamentos_DeveRetornarTodos()
    {
        var lancamentos = new List<Lancamento>
        {
            Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Credito, "Crédito A"),
            Lancamento.Criar(DateTime.Today, 50m, TipoLancamento.Debito, "Débito B")
        };
        _repository.ObterTodosAsync().Returns(lancamentos);

        var resultado = await _useCase.ObterTodosAsync();

        Assert.Equal(2, resultado.Count());
    }

    [Fact]
    public async Task ObterTodosAsync_QuandoVazio_DeveRetornarListaVazia()
    {
        _repository.ObterTodosAsync().Returns(Enumerable.Empty<Lancamento>());

        var resultado = await _useCase.ObterTodosAsync();

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ObterTodosAsync_DeveMapearTipoCorretamente()
    {
        var lancamentos = new List<Lancamento>
        {
            Lancamento.Criar(DateTime.Today, 200m, TipoLancamento.Credito, "Entrada"),
            Lancamento.Criar(DateTime.Today, 80m, TipoLancamento.Debito, "Saída")
        };
        _repository.ObterTodosAsync().Returns(lancamentos);

        var resultado = (await _useCase.ObterTodosAsync()).ToList();

        Assert.Equal("Credito", resultado[0].Tipo);
        Assert.Equal("Debito", resultado[1].Tipo);
    }

    // ─── ObterPorIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ObterPorIdAsync_QuandoExiste_DeveRetornarResponse()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 300m, TipoLancamento.Credito, "Venda");
        _repository.ObterPorIdAsync(lancamento.Id).Returns(lancamento);

        var resultado = await _useCase.ObterPorIdAsync(lancamento.Id);

        Assert.Equal(lancamento.Id, resultado.Id);
        Assert.Equal(300m, resultado.Valor);
        Assert.Equal("Credito", resultado.Tipo);
        Assert.Equal("Venda", resultado.Descricao);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoNaoExiste_DeveLancarNotFoundException()
    {
        var idInexistente = Guid.NewGuid();
        _repository.ObterPorIdAsync(idInexistente).Returns((Lancamento?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _useCase.ObterPorIdAsync(idInexistente));
    }

    // ─── ObterPorDataAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ObterPorDataAsync_DeveRetornarLancamentosDaData()
    {
        var data = new DateTime(2025, 6, 10);
        var lancamentos = new List<Lancamento>
        {
            Lancamento.Criar(data, 400m, TipoLancamento.Credito, "Pagamento"),
            Lancamento.Criar(data, 100m, TipoLancamento.Debito, "Compra")
        };
        _repository.ObterPorDataAsync(data).Returns(lancamentos);

        var resultado = await _useCase.ObterPorDataAsync(data);

        Assert.Equal(2, resultado.Count());
        Assert.All(resultado, r => Assert.Equal(data.Date, r.Data.Date));
    }

    [Fact]
    public async Task ObterPorDataAsync_QuandoSemLancamentos_DeveRetornarListaVazia()
    {
        var data = new DateTime(2025, 1, 1);
        _repository.ObterPorDataAsync(data).Returns(Enumerable.Empty<Lancamento>());

        var resultado = await _useCase.ObterPorDataAsync(data);

        Assert.Empty(resultado);
    }

    // ─── Mapeamento de response ───────────────────────────────────────────────

    [Fact]
    public async Task ObterPorIdAsync_ResponseDeveTerCriadoEmPreenchido()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Debito, "Teste");
        _repository.ObterPorIdAsync(lancamento.Id).Returns(lancamento);

        var resultado = await _useCase.ObterPorIdAsync(lancamento.Id);

        Assert.NotEqual(default, resultado.CriadoEm);
    }
}

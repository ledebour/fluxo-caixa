using FluxoCaixa.Consolidado.API.Application.UseCases;
using FluxoCaixa.Consolidado.API.Domain.Entities;
using FluxoCaixa.Consolidado.API.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FluxoCaixa.Consolidado.Tests.Application;

public class ConsultarConsolidadoUseCaseTests
{
    private readonly IConsolidadoRepository _repository = Substitute.For<IConsolidadoRepository>();
    private readonly IConsolidadoCache _cache = Substitute.For<IConsolidadoCache>();
    private readonly ILogger<ConsultarConsolidadoUseCase> _logger =
        Substitute.For<ILogger<ConsultarConsolidadoUseCase>>();
    private readonly ConsultarConsolidadoUseCase _useCase;

    public ConsultarConsolidadoUseCaseTests()
    {
        _useCase = new ConsultarConsolidadoUseCase(_repository, _cache, _logger);
    }

    // ─── Cache HIT ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ObterPorDataAsync_QuandoCacheHit_DeveRetornarDoCacheEMarcarVeioDoCache()
    {
        var data = new DateTime(2025, 4, 10);
        var consolidado = ConsolidadoDiario.Criar(data);
        consolidado.AplicarCredito(500m);
        _cache.ObterAsync(data).Returns(consolidado);

        var resultado = await _useCase.ObterPorDataAsync(data);

        Assert.NotNull(resultado);
        Assert.True(resultado!.VeioDoCache);
        Assert.Equal(500m, resultado.TotalCreditos);
    }

    [Fact]
    public async Task ObterPorDataAsync_QuandoCacheHit_NaoDeveConsultarRepositorio()
    {
        var data = new DateTime(2025, 4, 10);
        var consolidado = ConsolidadoDiario.Criar(data);
        _cache.ObterAsync(data).Returns(consolidado);

        await _useCase.ObterPorDataAsync(data);

        await _repository.DidNotReceive().ObterPorDataAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    // ─── Cache MISS ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ObterPorDataAsync_QuandoCacheMiss_DeveBuscarNoBancoEMarcarVeioDoCache()
    {
        var data = new DateTime(2025, 4, 10);
        var consolidado = ConsolidadoDiario.Criar(data);
        consolidado.AplicarCredito(300m);
        consolidado.AplicarDebito(100m);

        _cache.ObterAsync(data).Returns((ConsolidadoDiario?)null);
        _repository.ObterPorDataAsync(data).Returns(consolidado);

        var resultado = await _useCase.ObterPorDataAsync(data);

        Assert.NotNull(resultado);
        Assert.False(resultado!.VeioDoCache);
        Assert.Equal(300m, resultado.TotalCreditos);
        Assert.Equal(100m, resultado.TotalDebitos);
        Assert.Equal(200m, resultado.SaldoFinal);
    }

    [Fact]
    public async Task ObterPorDataAsync_QuandoCacheMiss_DeveSalvarNoCache()
    {
        var data = new DateTime(2025, 4, 10);
        var consolidado = ConsolidadoDiario.Criar(data);
        _cache.ObterAsync(data).Returns((ConsolidadoDiario?)null);
        _repository.ObterPorDataAsync(data).Returns(consolidado);

        await _useCase.ObterPorDataAsync(data);

        await _cache.Received(1).SalvarAsync(
            Arg.Any<ConsolidadoDiario>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObterPorDataAsync_QuandoNaoExisteNoBanco_DeveRetornarNull()
    {
        var data = new DateTime(2025, 4, 10);
        _cache.ObterAsync(data).Returns((ConsolidadoDiario?)null);
        _repository.ObterPorDataAsync(data).Returns((ConsolidadoDiario?)null);

        var resultado = await _useCase.ObterPorDataAsync(data);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterPorDataAsync_QuandoNaoExisteNoBanco_NaoDeveSalvarNoCache()
    {
        var data = new DateTime(2025, 4, 10);
        _cache.ObterAsync(data).Returns((ConsolidadoDiario?)null);
        _repository.ObterPorDataAsync(data).Returns((ConsolidadoDiario?)null);

        await _useCase.ObterPorDataAsync(data);

        await _cache.DidNotReceive().SalvarAsync(
            Arg.Any<ConsolidadoDiario>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    // ─── ObterPorPeriodoAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveAgregarTotaisCorretamente()
    {
        var inicio = new DateTime(2025, 4, 1);
        var fim = new DateTime(2025, 4, 3);

        var dia1 = ConsolidadoDiario.Criar(inicio);
        dia1.AplicarCredito(1000m);
        dia1.AplicarDebito(200m);

        var dia2 = ConsolidadoDiario.Criar(inicio.AddDays(1));
        dia2.AplicarCredito(500m);

        var dia3 = ConsolidadoDiario.Criar(fim);
        dia3.AplicarDebito(300m);

        _repository.ObterPorPeriodoAsync(inicio, fim).Returns(new[] { dia1, dia2, dia3 });

        var resultado = await _useCase.ObterPorPeriodoAsync(inicio, fim);

        Assert.Equal(1500m, resultado.TotalCreditos);
        Assert.Equal(500m, resultado.TotalDebitos);
        Assert.Equal(1000m, resultado.SaldoFinal);
        Assert.Equal(3, resultado.TotalDias);
        Assert.Equal(3, resultado.Dias.Count());
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_SemDados_DeveRetornarTotaisZerados()
    {
        var inicio = new DateTime(2025, 4, 1);
        var fim = new DateTime(2025, 4, 7);
        _repository.ObterPorPeriodoAsync(inicio, fim).Returns(Enumerable.Empty<ConsolidadoDiario>());

        var resultado = await _useCase.ObterPorPeriodoAsync(inicio, fim);

        Assert.Equal(0m, resultado.TotalCreditos);
        Assert.Equal(0m, resultado.TotalDebitos);
        Assert.Equal(0m, resultado.SaldoFinal);
        Assert.Equal(0, resultado.TotalDias);
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_ComInicioMaiorQueFim_DeveLancarArgumentException()
    {
        var inicio = new DateTime(2025, 4, 10);
        var fim = new DateTime(2025, 4, 5);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _useCase.ObterPorPeriodoAsync(inicio, fim));
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_MesmaDia_DeveRetornarUmDia()
    {
        var data = new DateTime(2025, 4, 5);
        var consolidado = ConsolidadoDiario.Criar(data);
        consolidado.AplicarCredito(100m);
        _repository.ObterPorPeriodoAsync(data, data).Returns(new[] { consolidado });

        var resultado = await _useCase.ObterPorPeriodoAsync(data, data);

        Assert.Equal(1, resultado.TotalDias);
        Assert.Equal(100m, resultado.TotalCreditos);
    }
}

using FluxoCaixa.Consolidado.API.Domain.Entities;
using FluxoCaixa.Consolidado.API.Infrastructure.Data;
using FluxoCaixa.Consolidado.API.Infrastructure.Repositories;
using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Lancamentos.API.Infrastructure.Data;
using FluxoCaixa.Lancamentos.API.Infrastructure.Repositories;
using FluxoCaixa.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FluxoCaixa.Consolidado.API.IntegrationTests.Repositories;

// ─── LancamentoRepository ─────────────────────────────────────────────────────

/// <summary>
/// Testa LancamentoRepository com EF Core InMemory.
/// Cobre CRUD completo, ordenação e filtros por data.
/// </summary>
public class LancamentoRepositoryTests : IDisposable
{
    private readonly LancamentosDbContext _context;
    private readonly LancamentoRepository _repository;

    public LancamentoRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseInMemoryDatabase($"LancRepo_{Guid.NewGuid()}")
            .Options;
        _context = new LancamentosDbContext(options);
        _repository = new LancamentoRepository(_context, Substitute.For<ILogger<LancamentoRepository>>());
    }

    // ─── ObterPorPeriodoAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveFiltrarLancamentosNoIntervalo()
    {
        var d1 = new DateTime(2025, 1, 10);
        var d2 = new DateTime(2025, 1, 15);
        var d3 = new DateTime(2025, 1, 20);

        await _repository.AdicionarAsync(Lancamento.Criar(d1, 100m, TipoLancamento.Credito, "Dia 10"));
        await _repository.AdicionarAsync(Lancamento.Criar(d2, 200m, TipoLancamento.Debito, "Dia 15"));
        await _repository.AdicionarAsync(Lancamento.Criar(d3, 300m, TipoLancamento.Credito, "Dia 20"));

        var resultado = (await _repository.ObterPorPeriodoAsync(d1, d2)).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.DoesNotContain(resultado, l => l.Descricao == "Dia 20");
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_SemLancamentosNoIntervalo_DeveRetornarVazio()
    {
        var resultado = await _repository.ObterPorPeriodoAsync(
            new DateTime(2000, 1, 1), new DateTime(2000, 1, 31));
        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveRetornarOrdenadoPorDataECriadoEm()
    {
        var data = new DateTime(2025, 3, 5);
        await _repository.AdicionarAsync(Lancamento.Criar(data, 300m, TipoLancamento.Credito, "Terceiro"));
        await Task.Delay(5); // garante CriadoEm distinto
        await _repository.AdicionarAsync(Lancamento.Criar(data, 100m, TipoLancamento.Credito, "Primeiro"));
        await Task.Delay(5);
        await _repository.AdicionarAsync(Lancamento.Criar(data, 200m, TipoLancamento.Debito, "Segundo"));

        var resultado = (await _repository.ObterPorPeriodoAsync(data, data)).ToList();

        Assert.Equal(3, resultado.Count);
        // OrderBy Data, ThenBy CriadoEm — primeiro inserido deve vir antes
        Assert.Equal("Terceiro", resultado[0].Descricao);
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_MesmoDia_DeveRetornarLancamentosDodia()
    {
        var data = new DateTime(2025, 6, 15);
        await _repository.AdicionarAsync(Lancamento.Criar(data, 50m, TipoLancamento.Credito, "A"));
        await _repository.AdicionarAsync(Lancamento.Criar(data, 75m, TipoLancamento.Debito, "B"));

        var resultado = (await _repository.ObterPorPeriodoAsync(data, data)).ToList();

        Assert.Equal(2, resultado.Count);
    }

    // ─── ObterTodosAsync — ordenação DESC ─────────────────────────────────────

    [Fact]
    public async Task ObterTodosAsync_DeveOrdenarPorDataDescendente()
    {
        var antiga = new DateTime(2025, 1, 1);
        var recente = new DateTime(2025, 12, 31);

        await _repository.AdicionarAsync(Lancamento.Criar(antiga, 100m, TipoLancamento.Credito, "Antiga"));
        await _repository.AdicionarAsync(Lancamento.Criar(recente, 200m, TipoLancamento.Debito, "Recente"));

        var resultado = (await _repository.ObterTodosAsync()).ToList();

        Assert.Equal("Recente", resultado[0].Descricao);
        Assert.Equal("Antiga", resultado[1].Descricao);
    }

    [Fact]
    public async Task AdicionarAsync_DevePersisteERecuperarLancamento()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 200m, TipoLancamento.Credito, "Persistir");

        await _repository.AdicionarAsync(lancamento);
        var encontrado = await _repository.ObterPorIdAsync(lancamento.Id);

        Assert.NotNull(encontrado);
        Assert.Equal(lancamento.Id, encontrado!.Id);
        Assert.Equal(200m, encontrado.Valor);
    }

    [Fact]
    public async Task ObterTodosAsync_DeveRetornarTodosEmOrdem()
    {
        var ontem = DateTime.Today.AddDays(-1);
        var hoje = DateTime.Today;
        await _repository.AdicionarAsync(Lancamento.Criar(ontem, 100m, TipoLancamento.Credito, "Ontem"));
        await _repository.AdicionarAsync(Lancamento.Criar(hoje, 200m, TipoLancamento.Debito, "Hoje"));

        var resultado = (await _repository.ObterTodosAsync()).ToList();

        Assert.Equal(2, resultado.Count);
        // Ordenado por data DESC — hoje primeiro
        Assert.Equal("Hoje", resultado[0].Descricao);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoNaoExiste_DeveRetornarNull()
    {
        var resultado = await _repository.ObterPorIdAsync(Guid.NewGuid());
        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterPorDataAsync_DeveFiltrarPorData()
    {
        var data = new DateTime(2025, 6, 10);
        var outraData = new DateTime(2025, 6, 11);
        await _repository.AdicionarAsync(Lancamento.Criar(data, 100m, TipoLancamento.Credito, "No dia"));
        await _repository.AdicionarAsync(Lancamento.Criar(outraData, 200m, TipoLancamento.Debito, "Outro dia"));

        var resultado = (await _repository.ObterPorDataAsync(data)).ToList();

        Assert.Single(resultado);
        Assert.Equal("No dia", resultado[0].Descricao);
    }

    [Fact]
    public async Task ObterPorDataAsync_QuandoSemLancamentosNaData_DeveRetornarVazio()
    {
        var resultado = await _repository.ObterPorDataAsync(new DateTime(2000, 1, 1));
        Assert.Empty(resultado);
    }

    [Fact]
    public async Task RemoverAsync_DeveRemoverDoContexto()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 150m, TipoLancamento.Debito, "Remover");
        await _repository.AdicionarAsync(lancamento);

        await _repository.RemoverAsync(lancamento);
        var resultado = await _repository.ObterPorIdAsync(lancamento.Id);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ExisteAsync_QuandoExiste_DeveRetornarTrue()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Credito, "Existe");
        await _repository.AdicionarAsync(lancamento);

        var existe = await _repository.ExisteAsync(lancamento.Id);

        Assert.True(existe);
    }

    [Fact]
    public async Task ExisteAsync_QuandoNaoExiste_DeveRetornarFalse()
    {
        var existe = await _repository.ExisteAsync(Guid.NewGuid());
        Assert.False(existe);
    }

    [Fact]
    public async Task ObterTodosAsync_QuandoVazio_DeveRetornarListaVazia()
    {
        var resultado = await _repository.ObterTodosAsync();
        Assert.Empty(resultado);
    }

    public void Dispose() => _context.Dispose();
}

// ─── ConsolidadoRepository ────────────────────────────────────────────────────

/// <summary>
/// Testa ConsolidadoRepository com EF Core InMemory.
/// Cobre ObterPorData, ObterPorPeriodo e SalvarAsync (insert + update).
/// </summary>
public class ConsolidadoRepositoryTests : IDisposable
{
    private readonly ConsolidadoDbContext _context;
    private readonly ConsolidadoRepository _repository;

    public ConsolidadoRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseInMemoryDatabase($"ConsolidadoRepo_{Guid.NewGuid()}")
            .Options;
        _context = new ConsolidadoDbContext(options);
        _repository = new ConsolidadoRepository(_context);
    }

    [Fact]
    public async Task SalvarAsync_Insert_DevePersistirConsolidado()
    {
        var consolidado = ConsolidadoDiario.Criar(DateTime.Today);
        consolidado.AplicarCredito(500m);

        await _repository.SalvarAsync(consolidado);
        var encontrado = await _repository.ObterPorDataAsync(DateTime.Today);

        Assert.NotNull(encontrado);
        Assert.Equal(500m, encontrado!.TotalCreditos);
    }

    [Fact]
    public async Task SalvarAsync_Update_DeveAtualizarConsolidadoExistente()
    {
        var data = new DateTime(2025, 4, 10);
        var consolidado = ConsolidadoDiario.Criar(data);
        consolidado.AplicarCredito(300m);
        await _repository.SalvarAsync(consolidado);

        consolidado.AplicarCredito(200m);
        await _repository.SalvarAsync(consolidado);

        var encontrado = await _repository.ObterPorDataAsync(data);
        Assert.NotNull(encontrado);
        Assert.Equal(500m, encontrado!.TotalCreditos);
    }

    [Fact]
    public async Task ObterPorDataAsync_QuandoNaoExiste_DeveRetornarNull()
    {
        var resultado = await _repository.ObterPorDataAsync(new DateTime(2000, 1, 1));
        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveFiltrarIntervalo()
    {
        var dia1 = new DateTime(2025, 3, 1);
        var dia2 = new DateTime(2025, 3, 2);
        var dia3 = new DateTime(2025, 3, 3);

        var c1 = ConsolidadoDiario.Criar(dia1); c1.AplicarCredito(100m);
        var c2 = ConsolidadoDiario.Criar(dia2); c2.AplicarCredito(200m);
        var c3 = ConsolidadoDiario.Criar(dia3); c3.AplicarCredito(300m);

        await _repository.SalvarAsync(c1);
        await _repository.SalvarAsync(c2);
        await _repository.SalvarAsync(c3);

        var resultado = (await _repository.ObterPorPeriodoAsync(dia1, dia2)).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.DoesNotContain(resultado, r => r.TotalCreditos == 300m);
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_SemDados_DeveRetornarVazio()
    {
        var resultado = await _repository.ObterPorPeriodoAsync(
            new DateTime(2000, 1, 1), new DateTime(2000, 1, 31));
        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ObterPorPeriodoAsync_DeveRetornarOrdenadoPorData()
    {
        var base_ = new DateTime(2025, 5, 1);
        for (int i = 2; i >= 0; i--)
        {
            var c = ConsolidadoDiario.Criar(base_.AddDays(i));
            c.AplicarCredito(100m * (i + 1));
            await _repository.SalvarAsync(c);
        }

        var resultado = (await _repository.ObterPorPeriodoAsync(base_, base_.AddDays(2))).ToList();

        Assert.Equal(3, resultado.Count);
        Assert.True(resultado[0].Data <= resultado[1].Data);
        Assert.True(resultado[1].Data <= resultado[2].Data);
    }

    public void Dispose() => _context.Dispose();
}

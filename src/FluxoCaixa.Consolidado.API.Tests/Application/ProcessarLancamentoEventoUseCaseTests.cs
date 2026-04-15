using FluxoCaixa.Consolidado.API.Application.UseCases;
using FluxoCaixa.Consolidado.API.Domain.Interfaces;
using FluxoCaixa.Shared.Enums;
using FluxoCaixa.Shared.Events;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using FluxoCaixa.Consolidado.API.Domain.Entities;

namespace FluxoCaixa.Consolidado.Tests.Application;

/// <summary>
/// Testa o fluxo completo: evento RabbitMQ → ProcessarLancamentoEventoUseCase → consolidado atualizado.
/// </summary>
public class ProcessarLancamentoEventoUseCaseTests
{
    private readonly IConsolidadoRepository _repository = Substitute.For<IConsolidadoRepository>();
    private readonly IConsolidadoCache _cache = Substitute.For<IConsolidadoCache>();
    private readonly ILogger<ProcessarLancamentoEventoUseCase> _logger =
        Substitute.For<ILogger<ProcessarLancamentoEventoUseCase>>();
    private readonly ProcessarLancamentoEventoUseCase _useCase;

    public ProcessarLancamentoEventoUseCaseTests()
    {
        _useCase = new ProcessarLancamentoEventoUseCase(_repository, _cache, _logger);
    }

    [Fact]
    public async Task ProcessarCriado_Credito_DeveCriarConsolidadoEAplicarCredito()
    {
        var data = new DateTime(2025, 6, 15);
        _repository.ObterPorDataAsync(data).Returns((ConsolidadoDiario?)null);

        var evento = new LancamentoCriadoEvent
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = 500m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Venda"
        };

        await _useCase.ProcessarCriadoAsync(evento);

        // Deve salvar consolidado com crédito aplicado
        await _repository.Received(1).SalvarAsync(
            Arg.Is<ConsolidadoDiario>(c => c.TotalCreditos == 500m && c.TotalDebitos == 0m));

        // Deve invalidar o cache
        await _cache.Received(1).InvalidarAsync(data);
    }

    [Fact]
    public async Task ProcessarCriado_Debito_DeveAplicarDebito()
    {
        var data = DateTime.Today;
        var consolidadoExistente = ConsolidadoDiario.Criar(data);
        consolidadoExistente.AplicarCredito(1000m);
        _repository.ObterPorDataAsync(data).Returns(consolidadoExistente);

        var evento = new LancamentoCriadoEvent
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = 300m,
            Tipo = TipoLancamento.Debito,
            Descricao = "Compra de material"
        };

        await _useCase.ProcessarCriadoAsync(evento);

        await _repository.Received(1).SalvarAsync(
            Arg.Is<ConsolidadoDiario>(c => c.TotalDebitos == 300m && c.SaldoFinal == 700m));
    }

    [Fact]
    public async Task ProcessarRemovido_DeveEstornarEInvalidarCache()
    {
        var data = DateTime.Today;
        var consolidado = ConsolidadoDiario.Criar(data);
        consolidado.AplicarCredito(800m);
        _repository.ObterPorDataAsync(data).Returns(consolidado);

        var evento = new LancamentoRemovidoEvent
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = 800m,
            Tipo = TipoLancamento.Credito
        };

        await _useCase.ProcessarRemovidoAsync(evento);

        await _repository.Received(1).SalvarAsync(
            Arg.Is<ConsolidadoDiario>(c => c.TotalCreditos == 0m));
        await _cache.Received(1).InvalidarAsync(data);
    }

    // ─── Consolidado já existente ─────────────────────────────────────────────

    [Fact]
    public async Task ProcessarCriado_ConsolidadoJaExiste_DeveAcumularAoExistente()
    {
        var data = new DateTime(2025, 7, 1);
        var consolidadoExistente = ConsolidadoDiario.Criar(data);
        consolidadoExistente.AplicarCredito(1000m);
        consolidadoExistente.AplicarDebito(200m);
        _repository.ObterPorDataAsync(data).Returns(consolidadoExistente);

        var evento = new LancamentoCriadoEvent
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = 500m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Segundo crédito do dia"
        };

        await _useCase.ProcessarCriadoAsync(evento);

        await _repository.Received(1).SalvarAsync(
            Arg.Is<ConsolidadoDiario>(c =>
                c.TotalCreditos == 1500m &&
                c.TotalDebitos == 200m &&
                c.SaldoFinal == 1300m),
            Arg.Any<CancellationToken>());
    }

    // ─── Múltiplos eventos no mesmo dia ───────────────────────────────────────

    [Fact]
    public async Task ProcessarCriado_MultiplosEventosMesmoDia_DeveCriarConsolidadoUmaSoVez()
    {
        var data = new DateTime(2025, 8, 15);

        // Primeiro evento — sem consolidado ainda
        _repository.ObterPorDataAsync(data).Returns((ConsolidadoDiario?)null);
        await _useCase.ProcessarCriadoAsync(new LancamentoCriadoEvent
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Primeiro"
        });

        // Segundo evento — consolidado já existe (retornado pelo mock)
        var consolidadoCriado = ConsolidadoDiario.Criar(data);
        consolidadoCriado.AplicarCredito(100m);
        _repository.ObterPorDataAsync(data).Returns(consolidadoCriado);
        await _useCase.ProcessarCriadoAsync(new LancamentoCriadoEvent
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = 200m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Segundo"
        });

        await _repository.Received(2).SalvarAsync(Arg.Any<ConsolidadoDiario>(), Arg.Any<CancellationToken>());
        await _cache.Received(2).InvalidarAsync(data, Arg.Any<CancellationToken>());
    }

    // ─── Estorno via ProcessarRemovido ────────────────────────────────────────

    [Fact]
    public async Task ProcessarRemovido_Debito_DeveEstornarDebito()
    {
        var data = DateTime.Today;
        var consolidado = ConsolidadoDiario.Criar(data);
        consolidado.AplicarCredito(500m);
        consolidado.AplicarDebito(300m);
        _repository.ObterPorDataAsync(data).Returns(consolidado);

        var evento = new LancamentoRemovidoEvent
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = 300m,
            Tipo = TipoLancamento.Debito
        };

        await _useCase.ProcessarRemovidoAsync(evento);

        await _repository.Received(1).SalvarAsync(
            Arg.Is<ConsolidadoDiario>(c =>
                c.TotalDebitos == 0m &&
                c.TotalCreditos == 500m &&
                c.SaldoFinal == 500m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessarRemovido_ConsolidadoNaoExiste_DeveCriarZeradoEEstornar()
    {
        var data = new DateTime(2025, 9, 1);
        _repository.ObterPorDataAsync(data).Returns((ConsolidadoDiario?)null);

        var evento = new LancamentoRemovidoEvent
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = 100m,
            Tipo = TipoLancamento.Credito
        };

        // Não deve lançar exceção — cria consolidado zerado e aplica estorno
        await _useCase.ProcessarRemovidoAsync(evento);

        await _repository.Received(1).SalvarAsync(
            Arg.Is<ConsolidadoDiario>(c => c.TotalCreditos == 0m),
            Arg.Any<CancellationToken>());
    }

    // ─── Cache invalidação ────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessarCriado_SempreInvalidaCacheAposGravar()
    {
        var data = DateTime.Today;
        _repository.ObterPorDataAsync(data).Returns((ConsolidadoDiario?)null);

        await _useCase.ProcessarCriadoAsync(new LancamentoCriadoEvent
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = 100m,
            Tipo = TipoLancamento.Debito,
            Descricao = "Teste"
        });

        // Cache DEVE ser invalidado após salvar — garante consistência
        Received.InOrder(() =>
        {
            _repository.SalvarAsync(Arg.Any<ConsolidadoDiario>(), Arg.Any<CancellationToken>());
            _cache.InvalidarAsync(data, Arg.Any<CancellationToken>());
        });
    }
}

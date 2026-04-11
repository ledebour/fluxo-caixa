using FluxoCaixa.Consolidado.API.Application.UseCases;
using FluxoCaixa.Consolidado.API.Domain.Interfaces;
using FluxoCaixa.Shared.Enums;
using FluxoCaixa.Shared.Events;
using NSubstitute;
using FluxoCaixa.Consolidado.API.Domain.Entities;
using Xunit;
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
}

using FluxoCaixa.Lancamentos.API.Application.UseCases;
using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Lancamentos.API.Domain.Exceptions;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Shared.Enums;
using FluxoCaixa.Shared.Events;
using NSubstitute;
using Xunit;
namespace FluxoCaixa.Lancamentos.API.Tests.Application;

public class RemoverLancamentoUseCaseTests
{
    private readonly ILancamentoRepository _repository = Substitute.For<ILancamentoRepository>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly RemoverLancamentoUseCase _useCase;

    public RemoverLancamentoUseCaseTests()
    {
        _useCase = new RemoverLancamentoUseCase(_repository, _publisher);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoLancamentoExiste_DeveRemoverDoRepositorio()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 200m, TipoLancamento.Credito, "Teste");
        _repository.ObterPorIdAsync(lancamento.Id).Returns(lancamento);

        await _useCase.ExecutarAsync(lancamento.Id);

        await _repository.Received(1).RemoverAsync(lancamento, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_QuandoLancamentoExiste_DevePublicarEventoRemovido()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 150m, TipoLancamento.Debito, "Despesa");
        _repository.ObterPorIdAsync(lancamento.Id).Returns(lancamento);

        await _useCase.ExecutarAsync(lancamento.Id);

        await _publisher.Received(1).PublicarAsync(
            Arg.Is<LancamentoRemovidoEvent>(e =>
                e.Id == lancamento.Id &&
                e.Valor == 150m &&
                e.Tipo == TipoLancamento.Debito),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_QuandoLancamentoNaoExiste_DeveLancarNotFoundException()
    {
        var idInexistente = Guid.NewGuid();
        _repository.ObterPorIdAsync(idInexistente).Returns((Lancamento?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _useCase.ExecutarAsync(idInexistente));
    }

    [Fact]
    public async Task ExecutarAsync_QuandoLancamentoNaoExiste_NaoDeveRemoverNemPublicar()
    {
        var idInexistente = Guid.NewGuid();
        _repository.ObterPorIdAsync(idInexistente).Returns((Lancamento?)null);

        try { await _useCase.ExecutarAsync(idInexistente); } catch { /* expected */ }

        await _repository.DidNotReceive().RemoverAsync(Arg.Any<Lancamento>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublicarAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_EventoPublicado_DeveTerMesmaDataDoLancamento()
    {
        var data = new DateTime(2025, 3, 10);
        var lancamento = Lancamento.Criar(data, 500m, TipoLancamento.Credito, "Venda");
        _repository.ObterPorIdAsync(lancamento.Id).Returns(lancamento);

        await _useCase.ExecutarAsync(lancamento.Id);

        await _publisher.Received(1).PublicarAsync(
            Arg.Is<LancamentoRemovidoEvent>(e => e.Data.Date == data.Date),
            Arg.Any<CancellationToken>());
    }
}

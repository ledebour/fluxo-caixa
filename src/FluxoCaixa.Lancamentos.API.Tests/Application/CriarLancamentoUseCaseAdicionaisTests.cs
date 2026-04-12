using FluxoCaixa.Lancamentos.API.Application.DTOs;
using FluxoCaixa.Lancamentos.API.Application.UseCases;
using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Lancamentos.API.Domain.Exceptions;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Shared.Enums;
using FluxoCaixa.Shared.Events;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
namespace FluxoCaixa.Lancamentos.API.Tests.Application;

public class CriarLancamentoUseCaseAdicionaisTests
{
    private readonly ILancamentoRepository _repository = Substitute.For<ILancamentoRepository>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly CriarLancamentoUseCase _useCase;

    public CriarLancamentoUseCaseAdicionaisTests()
    {
        _useCase = new CriarLancamentoUseCase(_repository, _publisher);
    }

    [Fact]
    public async Task ExecutarAsync_ComDebito_DeveRetornarTipoDebito()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 75m,
            Tipo = TipoLancamento.Debito,
            Descricao = "Conta de luz"
        };

        var response = await _useCase.ExecutarAsync(request);

        Assert.Equal("Debito", response.Tipo);
        Assert.Equal(75m, response.Valor);
    }

    [Fact]
    public async Task ExecutarAsync_ResponseDeveTerDataCorreta()
    {
        var data = new DateTime(2025, 5, 20);
        var request = new CriarLancamentoRequest
        {
            Data = data,
            Valor = 500m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Recebimento"
        };

        var response = await _useCase.ExecutarAsync(request);

        Assert.Equal(data.Date, response.Data.Date);
    }

    [Fact]
    public async Task ExecutarAsync_EventoCriadoDeveTerMesmosValoresDoRequest()
    {
        var data = DateTime.Today;
        var request = new CriarLancamentoRequest
        {
            Data = data,
            Valor = 999m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Verificação de evento"
        };

        await _useCase.ExecutarAsync(request);

        await _publisher.Received(1).PublicarAsync(
            Arg.Is<LancamentoCriadoEvent>(e =>
                e.Valor == 999m &&
                e.Tipo == TipoLancamento.Credito &&
                e.Data.Date == data.Date &&
                e.Descricao == "Verificação de evento"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosDomainInvalidos_NaoDevePersistirNemPublicar()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today.AddDays(5), // data futura — DomainException
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Inválido"
        };

        await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecutarAsync(request));

        await _repository.DidNotReceive().AdicionarAsync(Arg.Any<Lancamento>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublicarAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_FalhaNoRepositorio_DevePropagarExcecao()
    {
        _repository.AdicionarAsync(Arg.Any<Lancamento>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Banco indisponível"));

        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Teste de falha"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecutarAsync(request));
    }

    [Fact]
    public async Task ExecutarAsync_CriadoEmDeveSerPreenchidoAutomaticamente()
    {
        var antes = DateTime.UtcNow.AddSeconds(-1);
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Timestamp test"
        };

        var response = await _useCase.ExecutarAsync(request);

        Assert.True(response.CriadoEm >= antes);
    }
}

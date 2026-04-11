using FluxoCaixa.Lancamentos.API.Application.DTOs;
using FluxoCaixa.Lancamentos.API.Application.UseCases;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Shared.Enums;
using NSubstitute;
using Xunit;

namespace FluxoCaixa.Lancamentos.API.Tests.Application;

public class CriarLancamentoUseCaseTests
{
    private readonly ILancamentoRepository _repository = Substitute.For<ILancamentoRepository>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly CriarLancamentoUseCase _useCase;

    public CriarLancamentoUseCaseTests()
    {
        _useCase = new CriarLancamentoUseCase(_repository, _publisher);
    }

    [Fact]
    public async Task ExecutarAsync_ComRequestValido_DeveRetornarResponse()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 250.00m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Pagamento recebido"
        };

        var response = await _useCase.ExecutarAsync(request);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(250.00m, response.Valor);
        Assert.Equal("Credito", response.Tipo);
    }

    [Fact]
    public async Task ExecutarAsync_DevePersistirNoRepositorio()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 100m,
            Tipo = TipoLancamento.Debito,
            Descricao = "Compra de material"
        };

        await _useCase.ExecutarAsync(request);

        await _repository.Received(1).AdicionarAsync(Arg.Any<Domain.Entities.Lancamento>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_DevePublicarEventoNoRabbitMQ()
    {
        var request = new CriarLancamentoRequest
        {
            Data = DateTime.Today,
            Valor = 300m,
            Tipo = TipoLancamento.Credito,
            Descricao = "Venda realizada"
        };

        await _useCase.ExecutarAsync(request);

        await _publisher.Received(1).PublicarAsync(
            Arg.Any<Shared.Events.LancamentoCriadoEvent>(),
            Arg.Any<CancellationToken>());
    }
}

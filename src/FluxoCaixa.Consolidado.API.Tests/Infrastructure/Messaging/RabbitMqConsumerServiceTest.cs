using System.Reflection;
using System.Text;
using System.Text.Json;
using FluxoCaixa.Consolidado.API.Application.UseCases;
using FluxoCaixa.Consolidado.API.Domain.Entities;
using FluxoCaixa.Consolidado.API.Domain.Interfaces;
using FluxoCaixa.Consolidado.API.Infrastructure.Messaging;
using FluxoCaixa.Shared.Events;
using FluxoCaixa.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RabbitMQ.Client.Events;
using Xunit;
namespace FluxoCaixa.Consolidado.Tests.Infrastructure.Messaging;

public class RabbitMqConsumerServiceTests
{
    private readonly RabbitMqSettings _settings = new()
    {
        Host = "localhost",
        Port = 5672,
        Username = "guest",
        Password = "guest",
        ExchangeName = "fluxo-caixa",
        QueueConsolidado = "consolidado.processar"
    };

    [Fact]
    public async Task ProcessarMensagemAsync_DeveChamarUseCaseCriado()
    {
        // Arrange: mocks das dependências
        var repo = Substitute.For<IConsolidadoRepository>();
        var cache = Substitute.For<IConsolidadoCache>();
        var useCaseLogger = Substitute.For<ILogger<ProcessarLancamentoEventoUseCase>>();
        var useCase = new ProcessarLancamentoEventoUseCase(repo, cache, useCaseLogger);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.GetService(typeof(ProcessarLancamentoEventoUseCase))
            .Returns(useCase);
        scopeFactory.CreateScope().Returns(scope);

        var logger = Substitute.For<ILogger<RabbitMqConsumerService>>();
        var service = new RabbitMqConsumerService(scopeFactory, Options.Create(_settings), logger);

        var evento = new LancamentoCriadoEvent { Id = Guid.NewGuid(), Valor = 100 };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento));
        var ea = new BasicDeliverEventArgs
        {
            RoutingKey = RabbitMqSettings.RoutingKeyLancamentoCriado,
            Body = new ReadOnlyMemory<byte>(body),
            DeliveryTag = 1
        };

        // Act: chamar via reflection
        var task = (Task)service.GetType()
            .GetMethod("ProcessarMensagemAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object[] { ea, CancellationToken.None })!;
        await task;

        // Assert: validar que o repo e cache foram chamados
        await repo.Received(1).SalvarAsync(Arg.Any<ConsolidadoDiario>(), Arg.Any<CancellationToken>());
        await cache.Received(1).InvalidarAsync(evento.Data, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessarMensagemAsync_DeveChamarUseCaseRemovido()
    {
        // Arrange: criar mocks das dependências
        var repo = Substitute.For<IConsolidadoRepository>();
        var cache = Substitute.For<IConsolidadoCache>();
        var useCaseLogger = Substitute.For<ILogger<ProcessarLancamentoEventoUseCase>>();
        var useCase = new ProcessarLancamentoEventoUseCase(repo, cache, useCaseLogger);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.GetService(typeof(ProcessarLancamentoEventoUseCase))
            .Returns(useCase);
        scopeFactory.CreateScope().Returns(scope);

        var logger = Substitute.For<ILogger<RabbitMqConsumerService>>();
        var service = new RabbitMqConsumerService(scopeFactory, Options.Create(_settings), logger);

        var evento = new LancamentoRemovidoEvent { Id = Guid.NewGuid(), Data = DateTime.UtcNow, Tipo = FluxoCaixa.Shared.Enums.TipoLancamento.Debito, Valor = 50 };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento));
        var ea = new BasicDeliverEventArgs
        {
            RoutingKey = RabbitMqSettings.RoutingKeyLancamentoRemovido,
            Body = new ReadOnlyMemory<byte>(body),
            DeliveryTag = 2
        };

        // Act: chamar via reflection
        var task = (Task)service.GetType()
            .GetMethod("ProcessarMensagemAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object[] { ea, CancellationToken.None })!;
        await task;

        // Assert: validar efeitos colaterais
        await repo.Received(1).SalvarAsync(Arg.Any<ConsolidadoDiario>(), Arg.Any<CancellationToken>());
        await cache.Received(1).InvalidarAsync(evento.Data, Arg.Any<CancellationToken>());
    }

}

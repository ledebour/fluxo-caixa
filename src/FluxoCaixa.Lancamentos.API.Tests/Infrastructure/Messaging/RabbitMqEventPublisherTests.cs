using System.Reflection;
using System.Text;
using System.Text.Json;
using FluxoCaixa.Lancamentos.API.Infrastructure.Messaging;
using FluxoCaixa.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;
namespace FluxoCaixa.Lancamentos.Tests.Infrastructure.Messaging;

public class RabbitMqEventPublisherTests
{
    private readonly RabbitMqSettings _settings = new()
    {
        Host = "localhost",
        Port = 5672,
        Username = "guest",
        Password = "guest",
        ExchangeName = "fluxo-caixa-exchange"
    };

    [Fact]
    public async Task PublicarAsync_DeveChamarBasicPublish_ComParametrosCorretos()
    {
        // Arrange
        var options = Options.Create(_settings);
        var logger = Substitute.For<ILogger<RabbitMqEventPublisher>>();

        var connection = Substitute.For<IConnection>();
        var channel = Substitute.For<IModel>();
        connection.CreateModel().Returns(channel);

        // Forçar o publisher a usar nosso mock de conexão
        var publisher = new RabbitMqEventPublisher(options, logger);
        publisher.GetType()
                 .GetField("_connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                 ?.SetValue(publisher, connection);
        publisher.GetType()
                 .GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                 ?.SetValue(publisher, channel);

        var evento = new { Id = 1, Valor = 100.0 };

        // Act
        await publisher.PublicarAsync(evento);

        // Assert
        var expectedJson = JsonSerializer.Serialize(evento);

        channel.Received(1).BasicPublish(
            _settings.ExchangeName,
            Arg.Any<string>(),
            Arg.Any<IBasicProperties>(),
            Arg.Is<ReadOnlyMemory<byte>>(rm =>
                Encoding.UTF8.GetString(rm.ToArray()) == expectedJson
            )
        );


    }

    [Fact]
    public async Task PublicarAsync_QuandoChannelFalha_DeveLogarErro()
    {
        var options = Options.Create(_settings);
        var logger = Substitute.For<ILogger<RabbitMqEventPublisher>>();

        var connection = Substitute.For<IConnection>();
        var channel = Substitute.For<IModel>();
        connection.CreateModel().Returns(channel);

        channel.When(c => c.BasicPublish(default!, default!, default!, default!))
               .Do(_ => throw new Exception("RabbitMQ error"));

        var publisher = new RabbitMqEventPublisher(options, logger);
        publisher.GetType()
                 .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)
                 ?.SetValue(publisher, connection);
        publisher.GetType()
                 .GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance)
                 ?.SetValue(publisher, channel);

        var evento = new { Id = 2, Valor = 200.0 };

        await publisher.PublicarAsync(evento);

        // Assert: apenas verificar que LogError foi chamado
        logger.ReceivedWithAnyArgs().LogError(default!, default!, default!, default!);
    }

}

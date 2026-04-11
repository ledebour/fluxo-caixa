using System.Text;
using System.Text.Json;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Shared.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FluxoCaixa.Lancamentos.API.Infrastructure.Messaging;

/// <summary>
/// Publicador de eventos via RabbitMQ.
/// Exchange tipo "topic" + routing keys semânticas.
///
/// DECISÃO: Falha no RabbitMQ NÃO reverte o lançamento já persistido.
/// Em produção usar Outbox Pattern para garantia total de entrega.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _lock = new();

    public RabbitMqEventPublisher(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        TentarConectar();
    }

    public async Task PublicarAsync<TEvent>(TEvent evento, CancellationToken ct = default) where TEvent : class
    {
        var routingKey = ObterRoutingKey<TEvent>();
        var json = JsonSerializer.Serialize(evento);
        var body = Encoding.UTF8.GetBytes(json);

        try
        {
            EnsureChannelAberto();

            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";
            props.Type = typeof(TEvent).Name;
            props.MessageId = Guid.NewGuid().ToString();
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: routingKey,
                basicProperties: props,
                body: body);

            _logger.LogInformation("[RabbitMQ] {Tipo} publicado | RoutingKey: {Key}", typeof(TEvent).Name, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RabbitMQ] FALHA ao publicar {Tipo}. Payload: {Payload}", typeof(TEvent).Name, json);
        }

        await Task.CompletedTask;
    }

    private void TentarConectar()
    {
        try
        {
            lock (_lock)
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.Host,
                    Port = _settings.Port,
                    UserName = _settings.Username,
                    Password = _settings.Password,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection("FluxoCaixa.Lancamentos.Publisher");
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(
                    exchange: _settings.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                _logger.LogInformation("[RabbitMQ] Conectado em {Host}:{Port}", _settings.Host, _settings.Port);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RabbitMQ] Falha na conexão inicial. Tentará novamente ao publicar.");
        }
    }

    private void EnsureChannelAberto()
    {
        if (_channel is { IsOpen: true }) return;
        lock (_lock)
        {
            if (_channel is { IsOpen: true }) return;
            TentarConectar();
        }
    }

    private static string ObterRoutingKey<TEvent>() => typeof(TEvent).Name switch
    {
        "LancamentoCriadoEvent"   => RabbitMqSettings.RoutingKeyLancamentoCriado,
        "LancamentoRemovidoEvent" => RabbitMqSettings.RoutingKeyLancamentoRemovido,
        var nome                  => nome.ToLowerInvariant()
    };

    public void Dispose()
    {
        _channel?.Close(); _channel?.Dispose();
        _connection?.Close(); _connection?.Dispose();
    }
}

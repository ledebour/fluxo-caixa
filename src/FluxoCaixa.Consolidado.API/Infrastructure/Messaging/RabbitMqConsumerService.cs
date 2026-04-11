using System.Text;
using System.Text.Json;
using FluxoCaixa.Consolidado.API.Application.UseCases;
using FluxoCaixa.Shared.Events;
using FluxoCaixa.Shared.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FluxoCaixa.Consolidado.API.Infrastructure.Messaging;

/// <summary>
/// Background Service que consome eventos do RabbitMQ e processa o consolidado.
///
/// Topologia:
///   Exchange "fluxo-caixa" (topic)
///     ├── lancamento.criado   ──► queue "consolidado.processar"
///     └── lancamento.removido ──► queue "consolidado.processar"
///
/// Garantias:
///   - Prefetch = 1: processa uma mensagem por vez (evita sobrecarga)
///   - ACK manual: mensagem só é removida da fila após processamento bem-sucedido
///   - NACK + requeue: em caso de erro, mensagem volta para a fila (retry automático)
///   - Reconnect: AutomaticRecoveryEnabled reconecta se o broker cair
/// </summary>
public class RabbitMqConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Aguarda o app estar totalmente iniciado antes de conectar
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        ConectarERregistrarConsumer(stoppingToken);

        // Mantém o BackgroundService vivo
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void ConectarERregistrarConsumer(CancellationToken ct)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection("FluxoCaixa.Consolidado.Consumer");
            _channel = _connection.CreateModel();

            // Exchange (idempotente — safe declarar sempre)
            _channel.ExchangeDeclare(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Queue principal
            _channel.QueueDeclare(
                queue: _settings.QueueConsolidado,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bindings: escuta ambos os eventos de lançamento
            _channel.QueueBind(_settings.QueueConsolidado, _settings.ExchangeName,
                RabbitMqSettings.RoutingKeyLancamentoCriado);
            _channel.QueueBind(_settings.QueueConsolidado, _settings.ExchangeName,
                RabbitMqSettings.RoutingKeyLancamentoRemovido);

            // Processa 1 mensagem por vez — evita sobrecarga
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) => await ProcessarMensagemAsync(ea, ct);

            _channel.BasicConsume(
                queue: _settings.QueueConsolidado,
                autoAck: false,      // ACK manual — só confirma após processar
                consumer: consumer);

            _logger.LogInformation(
                "[RabbitMQ] Consumer registrado na queue '{Queue}'. Aguardando eventos...",
                _settings.QueueConsolidado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RabbitMQ] Falha ao iniciar consumer. Serviço de consulta continua disponível.");
        }
    }

    private async Task ProcessarMensagemAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var routingKey = ea.RoutingKey;
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());

        _logger.LogInformation("[RabbitMQ] Mensagem recebida | RoutingKey: {Key}", routingKey);

        try
        {
            // Cria novo scope pois ProcessarLancamentoEventoUseCase é Scoped
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessarLancamentoEventoUseCase>();

            if (routingKey == RabbitMqSettings.RoutingKeyLancamentoCriado)
            {
                var evento = JsonSerializer.Deserialize<LancamentoCriadoEvent>(body)
                    ?? throw new InvalidOperationException("Falha ao desserializar LancamentoCriadoEvent.");
                await useCase.ProcessarCriadoAsync(evento, ct);
            }
            else if (routingKey == RabbitMqSettings.RoutingKeyLancamentoRemovido)
            {
                var evento = JsonSerializer.Deserialize<LancamentoRemovidoEvent>(body)
                    ?? throw new InvalidOperationException("Falha ao desserializar LancamentoRemovidoEvent.");
                await useCase.ProcessarRemovidoAsync(evento, ct);
            }
            else
            {
                _logger.LogWarning("[RabbitMQ] RoutingKey desconhecida: {Key}. Descartando.", routingKey);
            }

            // ACK: confirma processamento, remove da fila
            _channel?.BasicAck(ea.DeliveryTag, multiple: false);
            _logger.LogDebug("[RabbitMQ] ACK enviado para DeliveryTag {Tag}", ea.DeliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RabbitMQ] Erro ao processar mensagem. NACK + requeue. Payload: {Body}", body);

            // NACK: devolve para a fila para nova tentativa
            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Close(); _channel?.Dispose();
        _connection?.Close(); _connection?.Dispose();
        base.Dispose();
    }
}

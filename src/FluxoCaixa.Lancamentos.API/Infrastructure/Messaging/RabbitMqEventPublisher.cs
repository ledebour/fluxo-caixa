using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using System.Text;
using System.Text.Json;

namespace FluxoCaixa.Lancamentos.API.Infrastructure.Messaging;

/// <summary>
/// Publicador de eventos via RabbitMQ.
/// Implementação completa com retry e circuit breaker será adicionada no commit 6.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly IConfiguration _config;

    public RabbitMqEventPublisher(ILogger<RabbitMqEventPublisher> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task PublicarAsync<TEvent>(TEvent evento, CancellationToken ct = default) where TEvent : class
    {
        // Stub: implementação RabbitMQ completa no commit 6
        var json = JsonSerializer.Serialize(evento);
        _logger.LogInformation("[RabbitMQ-STUB] Publicando evento {Tipo}: {Payload}",
            typeof(TEvent).Name, json);

        await Task.CompletedTask;
    }

    public void Dispose() { }
}

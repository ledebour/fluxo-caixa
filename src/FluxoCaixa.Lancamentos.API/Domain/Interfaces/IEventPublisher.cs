namespace FluxoCaixa.Lancamentos.API.Domain.Interfaces;

/// <summary>
/// Abstração para publicação de eventos de domínio.
/// Desacopla o domínio da implementação concreta do RabbitMQ.
/// </summary>
public interface IEventPublisher
{
    Task PublicarAsync<TEvent>(TEvent evento, CancellationToken ct = default) where TEvent : class;
}

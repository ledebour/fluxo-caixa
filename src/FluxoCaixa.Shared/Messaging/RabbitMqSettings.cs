namespace FluxoCaixa.Shared.Messaging;

/// <summary>
/// Configurações do RabbitMQ lidas do appsettings.json.
/// Compartilhado entre os dois microserviços.
/// </summary>
public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "fluxo";
    public string Password { get; set; } = "fluxo123";
    public string ExchangeName { get; set; } = "fluxo-caixa";
    public string QueueLancamentos { get; set; } = "lancamentos.criados";
    public string QueueConsolidado { get; set; } = "consolidado.processar";

    // Routing keys
    public const string RoutingKeyLancamentoCriado = "lancamento.criado";
    public const string RoutingKeyLancamentoRemovido = "lancamento.removido";
}

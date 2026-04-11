using FluxoCaixa.Shared.Enums;

namespace FluxoCaixa.Shared.Events;

/// <summary>
/// Evento publicado no RabbitMQ quando um lançamento é removido.
/// Consumido pelo serviço de Consolidado para recalcular o saldo diário.
/// </summary>
public record LancamentoRemovidoEvent
{
    public Guid Id { get; init; }
    public DateTime Data { get; init; }
    public decimal Valor { get; init; }
    public TipoLancamento Tipo { get; init; }
    public DateTime OcorridoEm { get; init; } = DateTime.UtcNow;
}

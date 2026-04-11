using FluxoCaixa.Shared.Enums;

namespace FluxoCaixa.Shared.Events;

/// <summary>
/// Evento publicado no RabbitMQ quando um lançamento é criado.
/// Consumido pelo serviço de Consolidado para atualizar o saldo diário.
/// </summary>
public record LancamentoCriadoEvent
{
    public Guid Id { get; init; }
    public DateTime Data { get; init; }
    public decimal Valor { get; init; }
    public TipoLancamento Tipo { get; init; }
    public string Descricao { get; init; } = string.Empty;
    public DateTime OcorridoEm { get; init; } = DateTime.UtcNow;
}

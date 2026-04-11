using FluxoCaixa.Consolidado.API.Domain.Entities;

namespace FluxoCaixa.Consolidado.API.Domain.Interfaces;

/// <summary>
/// Abstração do cache Redis para o consolidado diário.
/// Permite trocar a implementação sem alterar o domínio (DIP).
/// </summary>
public interface IConsolidadoCache
{
    Task<ConsolidadoDiario?> ObterAsync(DateTime data, CancellationToken ct = default);
    Task SalvarAsync(ConsolidadoDiario consolidado, TimeSpan? ttl = null, CancellationToken ct = default);
    Task InvalidarAsync(DateTime data, CancellationToken ct = default);
    Task InvalidarPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
}

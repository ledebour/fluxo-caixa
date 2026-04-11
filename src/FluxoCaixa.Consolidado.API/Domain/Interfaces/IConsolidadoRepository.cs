using FluxoCaixa.Consolidado.API.Domain.Entities;

namespace FluxoCaixa.Consolidado.API.Domain.Interfaces;

public interface IConsolidadoRepository
{
    Task<ConsolidadoDiario?> ObterPorDataAsync(DateTime data, CancellationToken ct = default);
    Task<IEnumerable<ConsolidadoDiario>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task SalvarAsync(ConsolidadoDiario consolidado, CancellationToken ct = default);
}

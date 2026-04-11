using FluxoCaixa.Lancamentos.API.Domain.Entities;

namespace FluxoCaixa.Lancamentos.API.Domain.Interfaces;

/// <summary>
/// Contrato do repositório de lançamentos.
/// A camada de domínio depende desta abstração — não da implementação concreta.
/// Padrão: Dependency Inversion (SOLID).
/// </summary>
public interface ILancamentoRepository
{
    Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Lancamento>> ObterTodosAsync(CancellationToken ct = default);
    Task<IEnumerable<Lancamento>> ObterPorDataAsync(DateTime data, CancellationToken ct = default);
    Task<IEnumerable<Lancamento>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task AdicionarAsync(Lancamento lancamento, CancellationToken ct = default);
    Task RemoverAsync(Lancamento lancamento, CancellationToken ct = default);
    Task<bool> ExisteAsync(Guid id, CancellationToken ct = default);
}

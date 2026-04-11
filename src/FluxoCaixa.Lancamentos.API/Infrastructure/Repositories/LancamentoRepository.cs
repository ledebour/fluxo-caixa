using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Lancamentos.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Lancamentos.API.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório usando EF Core + PostgreSQL.
/// Detalhes de mapeamento e otimizações serão adicionados no commit 4.
/// </summary>
public class LancamentoRepository : ILancamentoRepository
{
    private readonly LancamentosDbContext _context;

    public LancamentoRepository(LancamentosDbContext context)
    {
        _context = context;
    }

    public async Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Lancamentos.FindAsync([id], ct);

    public async Task<IEnumerable<Lancamento>> ObterTodosAsync(CancellationToken ct = default) =>
        await _context.Lancamentos
            .OrderByDescending(l => l.Data)
            .ThenByDescending(l => l.CriadoEm)
            .ToListAsync(ct);

    public async Task<IEnumerable<Lancamento>> ObterPorDataAsync(DateTime data, CancellationToken ct = default) =>
        await _context.Lancamentos
            .Where(l => l.Data.Date == data.Date)
            .OrderBy(l => l.CriadoEm)
            .ToListAsync(ct);

    public async Task<IEnumerable<Lancamento>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default) =>
        await _context.Lancamentos
            .Where(l => l.Data.Date >= inicio.Date && l.Data.Date <= fim.Date)
            .OrderBy(l => l.Data)
            .ToListAsync(ct);

    public async Task AdicionarAsync(Lancamento lancamento, CancellationToken ct = default)
    {
        await _context.Lancamentos.AddAsync(lancamento, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoverAsync(Lancamento lancamento, CancellationToken ct = default)
    {
        _context.Lancamentos.Remove(lancamento);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExisteAsync(Guid id, CancellationToken ct = default) =>
        await _context.Lancamentos.AnyAsync(l => l.Id == id, ct);
}

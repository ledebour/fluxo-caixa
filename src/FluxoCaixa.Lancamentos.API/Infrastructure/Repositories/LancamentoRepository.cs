using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Lancamentos.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Lancamentos.API.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de lançamentos usando EF Core + PostgreSQL.
/// Utiliza AsNoTracking em queries de leitura para melhor performance.
/// </summary>
public class LancamentoRepository : ILancamentoRepository
{
    private readonly LancamentosDbContext _context;
    private readonly ILogger<LancamentoRepository> _logger;

    public LancamentoRepository(LancamentosDbContext context, ILogger<LancamentoRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Lancamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IEnumerable<Lancamento>> ObterTodosAsync(CancellationToken ct = default) =>
        await _context.Lancamentos
            .AsNoTracking()
            .OrderByDescending(l => l.Data)
            .ThenByDescending(l => l.CriadoEm)
            .ToListAsync(ct);

    public async Task<IEnumerable<Lancamento>> ObterPorDataAsync(DateTime data, CancellationToken ct = default) =>
        await _context.Lancamentos
            .AsNoTracking()
            .Where(l => l.Data.Date == data.Date)
            .OrderBy(l => l.CriadoEm)
            .ToListAsync(ct);

    public async Task<IEnumerable<Lancamento>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default) =>
        await _context.Lancamentos
            .AsNoTracking()
            .Where(l => l.Data.Date >= inicio.Date && l.Data.Date <= fim.Date)
            .OrderBy(l => l.Data)
            .ThenBy(l => l.CriadoEm)
            .ToListAsync(ct);

    public async Task AdicionarAsync(Lancamento lancamento, CancellationToken ct = default)
    {
        await _context.Lancamentos.AddAsync(lancamento, ct);
        var rows = await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Lançamento {Id} persistido. Rows afetadas: {Rows}", lancamento.Id, rows);
    }

    public async Task RemoverAsync(Lancamento lancamento, CancellationToken ct = default)
    {
        _context.Lancamentos.Remove(lancamento);
        var rows = await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Lançamento {Id} removido. Rows afetadas: {Rows}", lancamento.Id, rows);
    }

    public async Task<bool> ExisteAsync(Guid id, CancellationToken ct = default) =>
        await _context.Lancamentos.AnyAsync(l => l.Id == id, ct);
}

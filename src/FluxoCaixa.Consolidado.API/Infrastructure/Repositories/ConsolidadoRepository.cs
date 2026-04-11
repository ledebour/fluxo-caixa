using FluxoCaixa.Consolidado.API.Domain.Entities;
using FluxoCaixa.Consolidado.API.Domain.Interfaces;
using FluxoCaixa.Consolidado.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.API.Infrastructure.Repositories;

public class ConsolidadoRepository : IConsolidadoRepository
{
    private readonly ConsolidadoDbContext _context;

    public ConsolidadoRepository(ConsolidadoDbContext context)
    {
        _context = context;
    }

    public async Task<ConsolidadoDiario?> ObterPorDataAsync(DateTime data, CancellationToken ct = default) =>
        await _context.Consolidados
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Data.Date == data.Date, ct);

    public async Task<IEnumerable<ConsolidadoDiario>> ObterPorPeriodoAsync(
        DateTime inicio, DateTime fim, CancellationToken ct = default) =>
        await _context.Consolidados
            .AsNoTracking()
            .Where(c => c.Data.Date >= inicio.Date && c.Data.Date <= fim.Date)
            .OrderBy(c => c.Data)
            .ToListAsync(ct);

    public async Task SalvarAsync(ConsolidadoDiario consolidado, CancellationToken ct = default)
    {
        // Upsert: atualiza se já existe, insere se não existe
        var existe = await _context.Consolidados
            .AnyAsync(c => c.Id == consolidado.Id, ct);

        if (existe)
            _context.Consolidados.Update(consolidado);
        else
            await _context.Consolidados.AddAsync(consolidado, ct);

        await _context.SaveChangesAsync(ct);
    }
}

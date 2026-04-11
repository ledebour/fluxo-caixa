using FluxoCaixa.Lancamentos.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Lancamentos.API.Infrastructure.Data;

/// <summary>
/// Contexto do Entity Framework Core para o serviço de Lançamentos.
/// Mapeamento completo das entidades está no commit 4 (Persistência).
/// </summary>
public class LancamentosDbContext : DbContext
{
    public LancamentosDbContext(DbContextOptions<LancamentosDbContext> options)
        : base(options) { }

    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurações de mapeamento serão aplicadas no commit 4
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LancamentosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

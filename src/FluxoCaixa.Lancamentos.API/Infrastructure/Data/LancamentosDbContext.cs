using FluxoCaixa.Lancamentos.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Lancamentos.API.Infrastructure.Data;

/// <summary>
/// Contexto do Entity Framework Core para o serviço de Lançamentos.
/// Aplica automaticamente todas as IEntityTypeConfiguration do assembly.
/// </summary>
public class LancamentosDbContext : DbContext
{
    public LancamentosDbContext(DbContextOptions<LancamentosDbContext> options)
        : base(options) { }

    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema padrão no PostgreSQL
        modelBuilder.HasDefaultSchema("public");

        // Aplica todos os IEntityTypeConfiguration do assembly automaticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LancamentosDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Garante que todos os DateTime sejam persistidos como UTC no PostgreSQL
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamp with time zone");
    }
}

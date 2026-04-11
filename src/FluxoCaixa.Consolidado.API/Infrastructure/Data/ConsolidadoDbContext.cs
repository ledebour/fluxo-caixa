using FluxoCaixa.Consolidado.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.API.Infrastructure.Data;

public class ConsolidadoDbContext : DbContext
{
    public ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options)
        : base(options) { }

    public DbSet<ConsolidadoDiario> Consolidados => Set<ConsolidadoDiario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConsolidadoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamp with time zone");
    }
}

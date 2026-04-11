using FluxoCaixa.Consolidado.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxoCaixa.Consolidado.API.Infrastructure.Data.Configurations;

public class ConsolidadoDiarioConfiguration : IEntityTypeConfiguration<ConsolidadoDiario>
{
    public void Configure(EntityTypeBuilder<ConsolidadoDiario> builder)
    {
        builder.ToTable("consolidados_diarios");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.Data)
            .HasColumnName("data").HasColumnType("date").IsRequired();

        builder.Property(c => c.TotalCreditos)
            .HasColumnName("total_creditos").HasColumnType("numeric(15,2)").IsRequired();

        builder.Property(c => c.TotalDebitos)
            .HasColumnName("total_debitos").HasColumnType("numeric(15,2)").IsRequired();

        builder.Property(c => c.QuantidadeLancamentos)
            .HasColumnName("quantidade_lancamentos").IsRequired();

        builder.Property(c => c.AtualizadoEm)
            .HasColumnName("atualizado_em").IsRequired();

        // SaldoFinal é computado — não persiste na tabela
        builder.Ignore(c => c.SaldoFinal);

        // Índice único por data — um consolidado por dia
        builder.HasIndex(c => c.Data)
            .IsUnique()
            .HasDatabaseName("ix_consolidados_data_unique");
    }
}

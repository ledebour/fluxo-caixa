using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxoCaixa.Lancamentos.API.Infrastructure.Data.Configurations;

/// <summary>
/// Mapeamento da entidade Lancamento para a tabela do PostgreSQL.
/// Usa IEntityTypeConfiguration para manter o DbContext limpo (SRP).
/// </summary>
public class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");

        // ─── Chave primária ───────────────────────────────────────────
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // gerado pelo domínio via Guid.NewGuid()

        // ─── Colunas ──────────────────────────────────────────────────
        builder.Property(l => l.Data)
            .HasColumnName("data")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(l => l.Valor)
            .HasColumnName("valor")
            .HasColumnType("numeric(15,2)")
            .IsRequired();

        builder.Property(l => l.Tipo)
            .HasColumnName("tipo")
            .HasConversion<string>()   // persiste "Credito"/"Debito" como texto
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(l => l.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // ─── Índices para queries frequentes ─────────────────────────
        builder.HasIndex(l => l.Data)
            .HasDatabaseName("ix_lancamentos_data");

        builder.HasIndex(l => l.Tipo)
            .HasDatabaseName("ix_lancamentos_tipo");

        builder.HasIndex(l => new { l.Data, l.Tipo })
            .HasDatabaseName("ix_lancamentos_data_tipo");
    }
}

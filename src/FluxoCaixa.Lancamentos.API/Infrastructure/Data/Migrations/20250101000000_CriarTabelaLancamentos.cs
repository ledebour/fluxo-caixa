using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxoCaixa.Lancamentos.API.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class CriarTabelaLancamentos : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "public");

        migrationBuilder.CreateTable(
            name: "lancamentos",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                data = table.Column<DateTime>(type: "date", nullable: false),
                valor = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_lancamentos", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_lancamentos_data",
            schema: "public",
            table: "lancamentos",
            column: "data");

        migrationBuilder.CreateIndex(
            name: "ix_lancamentos_tipo",
            schema: "public",
            table: "lancamentos",
            column: "tipo");

        migrationBuilder.CreateIndex(
            name: "ix_lancamentos_data_tipo",
            schema: "public",
            table: "lancamentos",
            columns: new[] { "data", "tipo" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "lancamentos",
            schema: "public");
    }
}

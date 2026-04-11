using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxoCaixa.Consolidado.API.Infrastructure.Data.Migrations;

public partial class CriarTabelaConsolidados : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "public");

        migrationBuilder.CreateTable(
            name: "consolidados_diarios",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                data = table.Column<DateTime>(type: "date", nullable: false),
                total_creditos = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                total_debitos = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                quantidade_lancamentos = table.Column<int>(nullable: false),
                atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_consolidados_diarios", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_consolidados_data_unique",
            schema: "public",
            table: "consolidados_diarios",
            column: "data",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "consolidados_diarios", schema: "public");
    }
}

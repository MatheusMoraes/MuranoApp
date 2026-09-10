using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuranoApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProductWholesalePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Preco",
                table: "Products",
                newName: "PrecoVarejo");

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoAtacado",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantidadeMinimaAtacado",
                table: "Products",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecoAtacado",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "QuantidadeMinimaAtacado",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "PrecoVarejo",
                table: "Products",
                newName: "Preco");
        }
    }
}

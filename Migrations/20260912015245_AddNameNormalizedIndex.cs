using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuranoApp.Migrations
{
    /// <inheritdoc />
    public partial class AddNameNormalizedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomeNormalizado",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NomeNormalizado",
                table: "Categories",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Backfill: preenche o nome normalizado das linhas já existentes
            // antes de criar o índice único — sem isso, toda linha ficaria
            // com "" e o índice falharia ao ser criado (violação de
            // unicidade) assim que houvesse mais de um produto/categoria.
            migrationBuilder.Sql(
                "UPDATE \"Products\" SET \"NomeNormalizado\" = lower(regexp_replace(btrim(\"Nome\"), '\\s+', ' ', 'g'));");

            migrationBuilder.Sql(
                "UPDATE \"Categories\" SET \"NomeNormalizado\" = lower(regexp_replace(btrim(\"Nome\"), '\\s+', ' ', 'g'));");

            migrationBuilder.CreateIndex(
                name: "IX_Products_NomeNormalizado",
                table: "Products",
                column: "NomeNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_NomeNormalizado",
                table: "Categories",
                column: "NomeNormalizado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_NomeNormalizado",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_NomeNormalizado",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "NomeNormalizado",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NomeNormalizado",
                table: "Categories");
        }
    }
}

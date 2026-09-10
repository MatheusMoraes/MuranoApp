using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuranoApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProductEstoqueMinimo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstoqueMinimo",
                table: "Products",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstoqueMinimo",
                table: "Products");
        }
    }
}

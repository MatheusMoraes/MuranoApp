using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuranoApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagemPublicId",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagemUrl",
                table: "Products",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagemPublicId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImagemUrl",
                table: "Products");
        }
    }
}

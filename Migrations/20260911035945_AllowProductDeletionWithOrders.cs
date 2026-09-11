using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuranoApp.Migrations
{
    /// <inheritdoc />
    public partial class AllowProductDeletionWithOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProdutoId",
                table: "OrderItems");

            migrationBuilder.AlterColumn<int>(
                name: "ProdutoId",
                table: "OrderItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // Nullable por enquanto — preenchida com o nome real do produto
            // (via o ProdutoId ainda válido nesse momento) antes de virar
            // obrigatória, pra não zerar o histórico dos itens já existentes.
            migrationBuilder.AddColumn<string>(
                name: "NomeProduto",
                table: "OrderItems",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"OrderItems\" oi SET \"NomeProduto\" = p.\"Nome\" FROM \"Products\" p WHERE p.\"Id\" = oi.\"ProdutoId\";");

            // Segurança: se por algum motivo já existisse um item órfão
            // (não deveria, já que até aqui a FK era Cascade), não deixa
            // NomeProduto nulo.
            migrationBuilder.Sql(
                "UPDATE \"OrderItems\" SET \"NomeProduto\" = 'Produto removido' WHERE \"NomeProduto\" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "NomeProduto",
                table: "OrderItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProdutoId",
                table: "OrderItems",
                column: "ProdutoId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProdutoId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "NomeProduto",
                table: "OrderItems");

            migrationBuilder.AlterColumn<int>(
                name: "ProdutoId",
                table: "OrderItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProdutoId",
                table: "OrderItems",
                column: "ProdutoId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

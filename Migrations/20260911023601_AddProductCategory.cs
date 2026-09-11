using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MuranoApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Cria a tabela de categorias primeiro.
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            // 2. Categoria "coringa" pra reclassificar os produtos que já
            // existem, sem quebrar a obrigatoriedade de categoria.
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Nome", "Descricao", "CriadoEm" },
                values: new object[]
                {
                    "Geral",
                    "Categoria padrão para produtos ainda não classificados.",
                    DateTime.UtcNow
                });

            // 3. Adiciona a coluna como nullable primeiro, faz o backfill
            // pra "Geral" e só depois torna obrigatória — assim nenhuma
            // linha existente vira NULL/inválida no meio do caminho.
            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Products\" SET \"CategoriaId\" = (SELECT \"Id\" FROM \"Categories\" WHERE \"Nome\" = 'Geral' LIMIT 1) WHERE \"CategoriaId\" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "CategoriaId",
                table: "Products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoriaId",
                table: "Products",
                column: "CategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoriaId",
                table: "Products",
                column: "CategoriaId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoriaId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoriaId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}

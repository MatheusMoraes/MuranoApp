using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MuranoApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Email",
                table: "AdminUsers",
                column: "Email",
                unique: true);

            // Único usuário admin (sem rota de cadastro pública). O hash
            // abaixo é PBKDF2 salgado (PasswordHasher do ASP.NET Core) — não
            // é reversível, então é seguro manter aqui mesmo versionado.
            migrationBuilder.InsertData(
                table: "AdminUsers",
                columns: new[] { "Nome", "Email", "PasswordHash", "CriadoEm" },
                values: new object[]
                {
                    "Admin",
                    "misteriosdemurano@gmail.com",
                    "AQAAAAIAAYagAAAAEI17VYm+/NIe6BWifS6rEEsJAZeuIapAKrQaLVKLG7hfMdeMIBKPpTDc3UZ1qee4RQ==",
                    DateTime.UtcNow
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");
        }
    }
}

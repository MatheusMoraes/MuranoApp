using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuranoApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_NomeNormalizado",
                table: "Products");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "AdminUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: todo Product/Order já existente (criado antes dessa
            // mudança) passa a pertencer ao usuário já existente hoje — ele
            // continua vendo tudo que já tinha, igual ao padrão de
            // backfill-antes-de-constranger usado nas migrations anteriores
            // (AddProductCategory, AddNameNormalizedIndex etc.).
            migrationBuilder.Sql(@"
                UPDATE ""Products"" SET ""UserId"" = (SELECT ""Id"" FROM ""AdminUsers"" WHERE ""Email"" = 'misteriosdemurano@gmail.com');
                UPDATE ""Orders"" SET ""UserId"" = (SELECT ""Id"" FROM ""AdminUsers"" WHERE ""Email"" = 'misteriosdemurano@gmail.com');
            ");

            // Novo usuário "admin murano": acesso total, vê/edita qualquer
            // produto e pedido (ver Product.UserId/Order.UserId e os
            // Services correspondentes, que ignoram o dono quando IsAdmin).
            // Hash PBKDF2 (PasswordHasher do ASP.NET Core) de uma senha
            // temporária — a ideia é logar e trocar pela tela de perfil.
            migrationBuilder.InsertData(
                table: "AdminUsers",
                columns: new[] { "Nome", "Email", "PasswordHash", "IsAdmin", "CriadoEm" },
                values: new object[]
                {
                    "Admin Murano",
                    "adminmurano@misteriosdemurano.com",
                    "AQAAAAIAAYagAAAAEC5xNrFruv4V4kI3ppqNiQflJSlJc5j9sDOMWL+m/MWIolH273Axeu1LyurH2VbQpg==",
                    true,
                    DateTime.UtcNow
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_UserId_NomeNormalizado",
                table: "Products",
                columns: new[] { "UserId", "NomeNormalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AdminUsers_UserId",
                table: "Orders",
                column: "UserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AdminUsers_UserId",
                table: "Products",
                column: "UserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AdminUsers_UserId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_AdminUsers_UserId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_UserId_NomeNormalizado",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "AdminUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Products_NomeNormalizado",
                table: "Products",
                column: "NomeNormalizado",
                unique: true);
        }
    }
}

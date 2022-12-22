using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalliDO.Service.Data.Migrations
{
    public partial class DirectoryParent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalName",
                table: "Directories");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Directories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Directories_ParentId",
                table: "Directories",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Directories_Directories_ParentId",
                table: "Directories",
                column: "ParentId",
                principalTable: "Directories",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Directories_Directories_ParentId",
                table: "Directories");

            migrationBuilder.DropIndex(
                name: "IX_Directories_ParentId",
                table: "Directories");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Directories");

            migrationBuilder.AddColumn<string>(
                name: "OriginalName",
                table: "Directories",
                type: "text",
                nullable: true);
        }
    }
}

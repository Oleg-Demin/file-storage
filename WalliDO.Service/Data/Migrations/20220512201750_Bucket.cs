using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalliDO.Service.Data.Migrations
{
    public partial class Bucket : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BucketId",
                table: "Files",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bucket",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessPolicy = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bucket", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Files_BucketId",
                table: "Files",
                column: "BucketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Bucket_BucketId",
                table: "Files",
                column: "BucketId",
                principalTable: "Bucket",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Bucket_BucketId",
                table: "Files");

            migrationBuilder.DropTable(
                name: "Bucket");

            migrationBuilder.DropIndex(
                name: "IX_Files_BucketId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "BucketId",
                table: "Files");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalliDO.Service.Data.Migrations
{
    public partial class BucketKeyName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Buckets_BucketId",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_BucketId",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Buckets",
                table: "Buckets");

            migrationBuilder.DropColumn(
                name: "BucketId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Buckets");

            migrationBuilder.AddColumn<string>(
                name: "BucketName",
                table: "Files",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Buckets",
                table: "Buckets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Files_BucketName",
                table: "Files",
                column: "BucketName");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Buckets_BucketName",
                table: "Files",
                column: "BucketName",
                principalTable: "Buckets",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Buckets_BucketName",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_BucketName",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Buckets",
                table: "Buckets");

            migrationBuilder.DropColumn(
                name: "BucketName",
                table: "Files");

            migrationBuilder.AddColumn<Guid>(
                name: "BucketId",
                table: "Files",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Buckets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Buckets",
                table: "Buckets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Files_BucketId",
                table: "Files",
                column: "BucketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Buckets_BucketId",
                table: "Files",
                column: "BucketId",
                principalTable: "Buckets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

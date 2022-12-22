using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalliDO.Service.Data.Migrations
{
    public partial class FileBucketIdNotNull : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Buckets_BucketId",
                table: "Files");

            migrationBuilder.AlterColumn<Guid>(
                name: "BucketId",
                table: "Files",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Buckets_BucketId",
                table: "Files",
                column: "BucketId",
                principalTable: "Buckets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Buckets_BucketId",
                table: "Files");

            migrationBuilder.AlterColumn<Guid>(
                name: "BucketId",
                table: "Files",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Buckets_BucketId",
                table: "Files",
                column: "BucketId",
                principalTable: "Buckets",
                principalColumn: "Id");
        }
    }
}

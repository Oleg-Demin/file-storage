using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalliDO.Service.Data.Migrations
{
    public partial class FileBucketConnection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Bucket_BucketId",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bucket",
                table: "Bucket");

            migrationBuilder.RenameTable(
                name: "Bucket",
                newName: "Buckets");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Buckets",
                table: "Buckets",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Buckets_BucketId",
                table: "Files",
                column: "BucketId",
                principalTable: "Buckets",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Buckets_BucketId",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Buckets",
                table: "Buckets");

            migrationBuilder.RenameTable(
                name: "Buckets",
                newName: "Bucket");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bucket",
                table: "Bucket",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Bucket_BucketId",
                table: "Files",
                column: "BucketId",
                principalTable: "Bucket",
                principalColumn: "Id");
        }
    }
}

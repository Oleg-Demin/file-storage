using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalliDO.Service.Data.Migrations
{
    public partial class BucketAccessKeyToString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AccessPolicy",
                table: "Buckets",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AccessPolicy",
                table: "Buckets",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

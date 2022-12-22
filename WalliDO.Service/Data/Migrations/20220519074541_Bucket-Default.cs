using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalliDO.Service.Data.Migrations
{
    public partial class BucketDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Default",
                table: "Buckets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Default",
                table: "Buckets");
        }
    }
}

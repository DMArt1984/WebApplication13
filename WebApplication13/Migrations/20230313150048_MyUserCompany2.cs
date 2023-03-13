using Microsoft.EntityFrameworkCore.Migrations;

namespace FactPortal.Migrations
{
    public partial class MyUserCompany2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdCompany",
                schema: "Identity",
                table: "myUser",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdCompany",
                schema: "Identity",
                table: "myUser");
        }
    }
}

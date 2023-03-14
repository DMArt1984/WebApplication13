using Microsoft.EntityFrameworkCore.Migrations;

namespace FactPortal.Migrations.Business
{
    public partial class OrdersReport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Orders",
                table: "RepView",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Orders",
                table: "RepView");
        }
    }
}

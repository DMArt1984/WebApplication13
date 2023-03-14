using Microsoft.EntityFrameworkCore.Migrations;

namespace FactPortal.Migrations.Business
{
    public partial class TitleReport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "RepView",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "RepFormula",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "RepView");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "RepFormula");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace FactPortal.Migrations.Business
{
    public partial class MyBusiness1108 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DT",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "ReadyStep",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "groupFilesId",
                table: "Works");

            migrationBuilder.DropColumn(
                name: "myUserId",
                table: "Works");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "WorkSteps",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Steps",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "WorkSteps");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Steps");

            migrationBuilder.AddColumn<string>(
                name: "DT",
                table: "Works",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Works",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReadyStep",
                table: "Works",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Works",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "groupFilesId",
                table: "Works",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "myUserId",
                table: "Works",
                type: "text",
                nullable: true);
        }
    }
}

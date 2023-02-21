using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace FactPortal.Migrations.Business
{
    public partial class MyBusiness2302 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepColumn",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group = table.Column<string>(nullable: true),
                    element = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepColumn", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepCondition",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdColumn = table.Column<int>(nullable: false),
                    condition = table.Column<string>(nullable: true),
                    value1 = table.Column<string>(nullable: true),
                    value2 = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepCondition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepFormula",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    typeLeft = table.Column<bool>(nullable: false),
                    IdLeft = table.Column<int>(nullable: false),
                    AndOr = table.Column<bool>(nullable: false),
                    typeRight = table.Column<bool>(nullable: false),
                    IdRight = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepFormula", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepView",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdFormula = table.Column<int>(nullable: false),
                    IdColumns = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepView", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepColumn");

            migrationBuilder.DropTable(
                name: "RepCondition");

            migrationBuilder.DropTable(
                name: "RepFormula");

            migrationBuilder.DropTable(
                name: "RepView");
        }
    }
}

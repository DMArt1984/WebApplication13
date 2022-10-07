using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace FactPortal.Migrations
{
    public partial class MyMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Identity");

            migrationBuilder.CreateTable(
                name: "myRole",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_myRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "myUser",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Login = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedLogin = table.Column<string>(maxLength: 256, nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    FullName = table.Column<string>(nullable: true),
                    Photo = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_myUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "myRoleClaims",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_myRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_myRoleClaims_myRole_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "myRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "myUserClaims",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_myUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_myUserClaims_myUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "myUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "myUserRoles",
                schema: "Identity",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    RoleId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_myUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_myUserRoles_myRole_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "myRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_myUserRoles_myUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "myUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "Identity",
                table: "myRole",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_myRoleClaims_RoleId",
                schema: "Identity",
                table: "myRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "Identity",
                table: "myUser",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "Identity",
                table: "myUser",
                column: "NormalizedLogin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_myUserClaims_UserId",
                schema: "Identity",
                table: "myUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_myUserRoles_RoleId",
                schema: "Identity",
                table: "myUserRoles",
                column: "RoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "myRoleClaims",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "myUserClaims",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "myUserRoles",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "myRole",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "myUser",
                schema: "Identity");
        }
    }
}

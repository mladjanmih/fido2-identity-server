using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fido2IdentityServer.Identity.Migrations
{
    public partial class UserCertificates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserCertificates",
                schema: "AUTHENTICATION",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Thumbprint = table.Column<string>(nullable: true),
                    Certificate = table.Column<string>(nullable: true),
                    RegistrationDate = table.Column<DateTime>(nullable: false),
                    Subject = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCertificates_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "AUTHENTICATION",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCertificates_UserId",
                schema: "AUTHENTICATION",
                table: "UserCertificates",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCertificates",
                schema: "AUTHENTICATION");
        }
    }
}

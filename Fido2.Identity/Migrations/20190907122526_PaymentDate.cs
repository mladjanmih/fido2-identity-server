using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fido2IdentityServer.Identity.Migrations
{
    public partial class PaymentDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RequestDateTime",
                schema: "AUTHENTICATION",
                table: "Payments",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestDateTime",
                schema: "AUTHENTICATION",
                table: "Payments");
        }
    }
}

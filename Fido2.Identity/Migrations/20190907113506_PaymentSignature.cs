using Microsoft.EntityFrameworkCore.Migrations;

namespace Fido2IdentityServer.Identity.Migrations
{
    public partial class PaymentSignature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PublikKeyId",
                schema: "AUTHENTICATION",
                table: "FidoLogins",
                newName: "PublicKeyId");

            migrationBuilder.AddColumn<string>(
                name: "AuthenticatorData",
                schema: "AUTHENTICATION",
                table: "Payments",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientData",
                schema: "AUTHENTICATION",
                table: "Payments",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasSignature",
                schema: "AUTHENTICATION",
                table: "Payments",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PublicKeyId",
                schema: "AUTHENTICATION",
                table: "Payments",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                schema: "AUTHENTICATION",
                table: "Payments",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthenticatorData",
                schema: "AUTHENTICATION",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ClientData",
                schema: "AUTHENTICATION",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "HasSignature",
                schema: "AUTHENTICATION",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PublicKeyId",
                schema: "AUTHENTICATION",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Signature",
                schema: "AUTHENTICATION",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "PublicKeyId",
                schema: "AUTHENTICATION",
                table: "FidoLogins",
                newName: "PublikKeyId");
        }
    }
}

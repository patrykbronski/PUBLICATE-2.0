using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Publicate.Api.Migrations
{
    /// <inheritdoc />
    public partial class InstagramAuthMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConnectedAccounts_OwnerId_Platform_ExternalId",
                table: "ConnectedAccounts");

            migrationBuilder.AddColumn<string>(
                name: "AuthMethod",
                table: "ConnectedAccounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedAccounts_OwnerId_Platform_ExternalId_AuthMethod",
                table: "ConnectedAccounts",
                columns: new[] { "OwnerId", "Platform", "ExternalId", "AuthMethod" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConnectedAccounts_OwnerId_Platform_ExternalId_AuthMethod",
                table: "ConnectedAccounts");

            migrationBuilder.DropColumn(
                name: "AuthMethod",
                table: "ConnectedAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedAccounts_OwnerId_Platform_ExternalId",
                table: "ConnectedAccounts",
                columns: new[] { "OwnerId", "Platform", "ExternalId" },
                unique: true);
        }
    }
}

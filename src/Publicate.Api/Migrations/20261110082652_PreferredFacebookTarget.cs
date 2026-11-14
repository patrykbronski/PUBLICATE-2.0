using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Publicate.Api.Migrations
{
    /// <inheritdoc />
    public partial class PreferredFacebookTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PreferredFacebookTargetId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("68b82a6a-d7d8-41bd-a672-b302c88d0486"),
                column: "PreferredFacebookTargetId",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredFacebookTargetId",
                table: "Users");
        }
    }
}

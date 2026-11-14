using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Publicate.Api.Migrations
{
    /// <inheritdoc />
    public partial class TrackIncompleteUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Uploaded",
                table: "MediaAssets",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Uploaded",
                table: "MediaAssets");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Publicate.Api.Migrations
{
    /// <inheritdoc />
    public partial class ComposerDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredTargetsJson",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<bool>(
                name: "Archived",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ComposerJson",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Posts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Posts\" SET \"CreatedAt\" = CURRENT_TIMESTAMP;");
            // Preserve existing content/history, grant old unfinished drafts 48 hours from migration.
            migrationBuilder.Sql("""
                UPDATE "Posts" p SET "Archived" = true
                WHERE EXISTS (SELECT 1 FROM "PostVariants" v WHERE v."PostId" = p."Id")
                AND NOT EXISTS (SELECT 1 FROM "PostVariants" v WHERE v."PostId" = p."Id"
                  AND NOT EXISTS (SELECT 1 FROM "PublicationJobs" j WHERE j."VariantId" = v."Id" AND j."State" = 6));
                WITH ranked AS (
                  SELECT "Id", row_number() OVER (PARTITION BY "OwnerId" ORDER BY "UpdatedAt" DESC, "Id") AS n
                  FROM "Posts" WHERE NOT "Archived"
                ) UPDATE "Posts" SET "Archived" = true WHERE "Id" IN (SELECT "Id" FROM ranked WHERE n > 10);
                """);
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("68b82a6a-d7d8-41bd-a672-b302c88d0486"),
                column: "PreferredTargetsJson",
                value: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredTargetsJson",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Archived",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ComposerJson",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Posts");
        }
    }
}

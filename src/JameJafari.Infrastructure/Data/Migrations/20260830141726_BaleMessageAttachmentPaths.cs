using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BaleMessageAttachmentPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentPaths",
                table: "BaleMessages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "BaleMessageIds",
                table: "BaleMessages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql("""
                UPDATE BaleMessages
                SET AttachmentPaths = CONCAT('["', REPLACE(REPLACE(PhotoPath, '\', '\\'), '"', '\"'), '"]')
                WHERE PhotoPath IS NOT NULL AND LTRIM(RTRIM(PhotoPath)) <> '';
                """);

            migrationBuilder.Sql("""
                UPDATE BaleMessages
                SET BaleMessageIds = CONCAT('[', BaleMessageId, ']')
                WHERE BaleMessageId IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "BaleMessages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "BaleMessages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE BaleMessages
                SET PhotoPath = JSON_VALUE(AttachmentPaths, '$[0]')
                WHERE ISJSON(AttachmentPaths) = 1 AND JSON_VALUE(AttachmentPaths, '$[0]') IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE BaleMessages
                SET BaleMessageId = TRY_CAST(JSON_VALUE(BaleMessageIds, '$[0]') AS int)
                WHERE ISJSON(BaleMessageIds) = 1 AND JSON_VALUE(BaleMessageIds, '$[0]') IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "AttachmentPaths",
                table: "BaleMessages");

            migrationBuilder.DropColumn(
                name: "BaleMessageIds",
                table: "BaleMessages");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ForceAlterChatIdColumnsToNvarchar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('MessageChannels', 'ExternalChatId') IS NULL
                   AND COL_LENGTH('MessageChannels', 'ExternalChatId_new') IS NOT NULL
                    EXEC(N'EXEC sp_rename ''MessageChannels.ExternalChatId_new'', ''ExternalChatId'', ''COLUMN''');
                """,
                suppressTransaction: true);

            migrationBuilder.Sql("""
                IF COL_LENGTH('MessageChannels', 'ExternalChatId') IS NOT NULL
                   AND COL_LENGTH('MessageChannels', 'ExternalChatId_new') IS NOT NULL
                    ALTER TABLE MessageChannels DROP COLUMN ExternalChatId_new;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    WHERE c.object_id = OBJECT_ID('dbo.MessageChannels')
                      AND c.name = 'ExternalChatId'
                      AND t.name = 'bigint')
                    ALTER TABLE MessageChannels ALTER COLUMN ExternalChatId nvarchar(100) NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('BaleMessages', 'ChatId') IS NULL
                   AND COL_LENGTH('BaleMessages', 'ChatId_new') IS NOT NULL
                    EXEC(N'EXEC sp_rename ''BaleMessages.ChatId_new'', ''ChatId'', ''COLUMN''');
                """,
                suppressTransaction: true);

            migrationBuilder.Sql("""
                IF COL_LENGTH('BaleMessages', 'ChatId') IS NOT NULL
                   AND COL_LENGTH('BaleMessages', 'ChatId_new') IS NOT NULL
                    ALTER TABLE BaleMessages DROP COLUMN ChatId_new;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.columns c
                    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                    WHERE c.object_id = OBJECT_ID('dbo.BaleMessages')
                      AND c.name = 'ChatId'
                      AND t.name = 'bigint')
                    ALTER TABLE BaleMessages ALTER COLUMN ChatId nvarchar(100) NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

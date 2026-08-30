using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepairExternalChatIdStringColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent repair when ExternalChatIdToString was recorded but column stayed bigint
            migrationBuilder.Sql("""
                IF TYPE_NAME(COLUMNPROPERTY(OBJECT_ID('MessageChannels'), 'ExternalChatId', 'SystemTypeId')) = 'bigint'
                   AND COL_LENGTH('MessageChannels', 'ExternalChatId_new') IS NULL
                    ALTER TABLE MessageChannels ADD ExternalChatId_new nvarchar(100) NOT NULL
                        CONSTRAINT DF_MessageChannels_ExternalChatId_new DEFAULT '';
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('MessageChannels', 'ExternalChatId_new') IS NOT NULL
                   AND COL_LENGTH('MessageChannels', 'ExternalChatId') IS NOT NULL
                    EXEC(N'UPDATE MessageChannels SET ExternalChatId_new = CAST(ExternalChatId AS nvarchar(100))');
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('MessageChannels', 'ExternalChatId_new') IS NOT NULL
                   AND EXISTS (
                       SELECT 1 FROM sys.default_constraints
                       WHERE parent_object_id = OBJECT_ID('MessageChannels')
                         AND name = 'DF_MessageChannels_ExternalChatId_new')
                    ALTER TABLE MessageChannels DROP CONSTRAINT DF_MessageChannels_ExternalChatId_new;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('MessageChannels', 'ExternalChatId_new') IS NOT NULL
                   AND TYPE_NAME(COLUMNPROPERTY(OBJECT_ID('MessageChannels'), 'ExternalChatId', 'SystemTypeId')) = 'bigint'
                    ALTER TABLE MessageChannels DROP COLUMN ExternalChatId;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('MessageChannels', 'ExternalChatId_new') IS NOT NULL
                   AND COL_LENGTH('MessageChannels', 'ExternalChatId') IS NULL
                    EXEC(N'EXEC sp_rename ''MessageChannels.ExternalChatId_new'', ''ExternalChatId'', ''COLUMN''');
                """,
                suppressTransaction: true);

            migrationBuilder.Sql("""
                IF TYPE_NAME(COLUMNPROPERTY(OBJECT_ID('BaleMessages'), 'ChatId', 'SystemTypeId')) = 'bigint'
                   AND COL_LENGTH('BaleMessages', 'ChatId_new') IS NULL
                    ALTER TABLE BaleMessages ADD ChatId_new nvarchar(100) NOT NULL
                        CONSTRAINT DF_BaleMessages_ChatId_new DEFAULT '';
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('BaleMessages', 'ChatId_new') IS NOT NULL
                   AND COL_LENGTH('BaleMessages', 'ChatId') IS NOT NULL
                    EXEC(N'UPDATE BaleMessages SET ChatId_new = CASE WHEN ChatId = 0 THEN '''' ELSE CAST(ChatId AS nvarchar(100)) END');
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('BaleMessages', 'ChatId_new') IS NOT NULL
                   AND EXISTS (
                       SELECT 1 FROM sys.default_constraints
                       WHERE parent_object_id = OBJECT_ID('BaleMessages')
                         AND name = 'DF_BaleMessages_ChatId_new')
                    ALTER TABLE BaleMessages DROP CONSTRAINT DF_BaleMessages_ChatId_new;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('BaleMessages', 'ChatId_new') IS NOT NULL
                   AND TYPE_NAME(COLUMNPROPERTY(OBJECT_ID('BaleMessages'), 'ChatId', 'SystemTypeId')) = 'bigint'
                    ALTER TABLE BaleMessages DROP COLUMN ChatId;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('BaleMessages', 'ChatId_new') IS NOT NULL
                   AND COL_LENGTH('BaleMessages', 'ChatId') IS NULL
                    EXEC(N'EXEC sp_rename ''BaleMessages.ChatId_new'', ''ChatId'', ''COLUMN''');
                """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRubikaMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE bm
                SET BaleMessageIds = ISNULL((
                    SELECT CONCAT('[', STRING_AGG('"' + CAST([value] AS nvarchar(64)) + '"', ','), ']')
                    FROM OPENJSON(bm.BaleMessageIds)
                ), '[]')
                FROM BaleMessages bm
                WHERE ISJSON(bm.BaleMessageIds) = 1
                  AND bm.BaleMessageIds LIKE '%[0-9]%'
                  AND bm.BaleMessageIds NOT LIKE '%"%';

                ALTER TABLE BaleMessages ALTER COLUMN BaleMessageId nvarchar(64) NULL;
                """);

            migrationBuilder.AddColumn<string>(
                name: "RubikaChatId",
                table: "Persons",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RubikaBotStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastOffsetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubikaBotStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RubikaContactLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NormalizedPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChatId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: true),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubikaContactLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubikaContactLinks_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RubikaContactLinks_ChatId",
                table: "RubikaContactLinks",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_RubikaContactLinks_NormalizedPhone",
                table: "RubikaContactLinks",
                column: "NormalizedPhone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RubikaContactLinks_PersonId",
                table: "RubikaContactLinks",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RubikaBotStates");

            migrationBuilder.DropTable(
                name: "RubikaContactLinks");

            migrationBuilder.DropColumn(
                name: "RubikaChatId",
                table: "Persons");

            migrationBuilder.Sql("""
                UPDATE bm
                SET BaleMessageIds = ISNULL((
                    SELECT CONCAT('[', STRING_AGG(CAST([value] AS nvarchar(64)), ','), ']')
                    FROM OPENJSON(bm.BaleMessageIds)
                ), '[]')
                FROM BaleMessages bm
                WHERE ISJSON(bm.BaleMessageIds) = 1;

                ALTER TABLE BaleMessages ALTER COLUMN BaleMessageId int NULL;
                """);
        }
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using JameJafari.Infrastructure.Data;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260913200000_AddTelegramMessaging")]
public partial class AddTelegramMessaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "TelegramChatId",
            table: "Persons",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "TelegramBotStates",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                LastUpdateId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TelegramBotStates", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TelegramContactLinks",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                NormalizedPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                ChatId = table.Column<long>(type: "bigint", nullable: false),
                PersonId = table.Column<int>(type: "int", nullable: true),
                LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TelegramContactLinks", x => x.Id);
                table.ForeignKey(
                    name: "FK_TelegramContactLinks_Persons_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Persons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TelegramContactLinks_ChatId",
            table: "TelegramContactLinks",
            column: "ChatId");

        migrationBuilder.CreateIndex(
            name: "IX_TelegramContactLinks_NormalizedPhone",
            table: "TelegramContactLinks",
            column: "NormalizedPhone",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TelegramContactLinks_PersonId",
            table: "TelegramContactLinks",
            column: "PersonId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TelegramBotStates");
        migrationBuilder.DropTable(name: "TelegramContactLinks");
        migrationBuilder.DropColumn(name: "TelegramChatId", table: "Persons");
    }
}

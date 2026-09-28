using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using JameJafari.Infrastructure.Data;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260928220000_AddWhatsAppMessaging")]
public partial class AddWhatsAppMessaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "WhatsAppChatId",
            table: "Persons",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "WhatsAppContactLinks",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                NormalizedPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                ChatId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                PersonId = table.Column<int>(type: "int", nullable: true),
                LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WhatsAppContactLinks", x => x.Id);
                table.ForeignKey(
                    name: "FK_WhatsAppContactLinks_Persons_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Persons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WhatsAppContactLinks_ChatId",
            table: "WhatsAppContactLinks",
            column: "ChatId");

        migrationBuilder.CreateIndex(
            name: "IX_WhatsAppContactLinks_NormalizedPhone",
            table: "WhatsAppContactLinks",
            column: "NormalizedPhone",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WhatsAppContactLinks_PersonId",
            table: "WhatsAppContactLinks",
            column: "PersonId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "WhatsAppContactLinks");
        migrationBuilder.DropColumn(name: "WhatsAppChatId", table: "Persons");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BaleMobileFirstMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeletedFromBale",
                table: "BaleMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TargetMobile",
                table: "BaleMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BaleContactLinks",
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
                    table.PrimaryKey("PK_BaleContactLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaleContactLinks_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaleContactLinks_ChatId",
                table: "BaleContactLinks",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_BaleContactLinks_NormalizedPhone",
                table: "BaleContactLinks",
                column: "NormalizedPhone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaleContactLinks_PersonId",
                table: "BaleContactLinks",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaleContactLinks");

            migrationBuilder.DropColumn(
                name: "DeletedFromBale",
                table: "BaleMessages");

            migrationBuilder.DropColumn(
                name: "TargetMobile",
                table: "BaleMessages");
        }
    }
}

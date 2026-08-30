using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBaleMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BaleChatId",
                table: "Persons",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BaleMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    TargetKind = table.Column<int>(type: "int", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    BaleMessageId = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    LinkUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LinkLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhotoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IncomeTransactionId = table.Column<int>(type: "int", nullable: true),
                    PersonId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaleMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaleMessages_IncomeTransactions_IncomeTransactionId",
                        column: x => x.IncomeTransactionId,
                        principalTable: "IncomeTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BaleMessages_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaleMessages_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaleMessages_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaleMessages_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaleMessages_CreatedById",
                table: "BaleMessages",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_BaleMessages_DeletedById",
                table: "BaleMessages",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_BaleMessages_IncomeTransactionId",
                table: "BaleMessages",
                column: "IncomeTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_BaleMessages_PersonId",
                table: "BaleMessages",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_BaleMessages_Status_CreatedAt",
                table: "BaleMessages",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BaleMessages_UpdatedById",
                table: "BaleMessages",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaleMessages");

            migrationBuilder.DropColumn(
                name: "BaleChatId",
                table: "Persons");
        }
    }
}

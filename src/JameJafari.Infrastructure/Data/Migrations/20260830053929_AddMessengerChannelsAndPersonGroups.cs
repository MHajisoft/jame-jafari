using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessengerChannelsAndPersonGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BroadcastBatchId",
                table: "BaleMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MessageChannelId",
                table: "BaleMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MessengerKind",
                table: "BaleMessages",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PersonGroupId",
                table: "BaleMessages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MessageChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MessengerKind = table.Column<int>(type: "int", nullable: false),
                    ExternalChatId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_MessageChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageChannels_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageChannels_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageChannels_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PersonGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonGroups_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonGroups_Users_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonGroups_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonGroupMembers",
                columns: table => new
                {
                    PersonGroupId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonGroupMembers", x => new { x.PersonGroupId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_PersonGroupMembers_PersonGroups_PersonGroupId",
                        column: x => x.PersonGroupId,
                        principalTable: "PersonGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonGroupMembers_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaleMessages_BroadcastBatchId",
                table: "BaleMessages",
                column: "BroadcastBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BaleMessages_MessageChannelId",
                table: "BaleMessages",
                column: "MessageChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_BaleMessages_PersonGroupId",
                table: "BaleMessages",
                column: "PersonGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageChannels_CreatedById",
                table: "MessageChannels",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MessageChannels_DeletedById",
                table: "MessageChannels",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MessageChannels_MessengerKind_IsActive",
                table: "MessageChannels",
                columns: new[] { "MessengerKind", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageChannels_UpdatedById",
                table: "MessageChannels",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroupMembers_PersonId",
                table: "PersonGroupMembers",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroups_CreatedById",
                table: "PersonGroups",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroups_DeletedById",
                table: "PersonGroups",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroups_UpdatedById",
                table: "PersonGroups",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_BaleMessages_MessageChannels_MessageChannelId",
                table: "BaleMessages",
                column: "MessageChannelId",
                principalTable: "MessageChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BaleMessages_PersonGroups_PersonGroupId",
                table: "BaleMessages",
                column: "PersonGroupId",
                principalTable: "PersonGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaleMessages_MessageChannels_MessageChannelId",
                table: "BaleMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_BaleMessages_PersonGroups_PersonGroupId",
                table: "BaleMessages");

            migrationBuilder.DropTable(
                name: "MessageChannels");

            migrationBuilder.DropTable(
                name: "PersonGroupMembers");

            migrationBuilder.DropTable(
                name: "PersonGroups");

            migrationBuilder.DropIndex(
                name: "IX_BaleMessages_BroadcastBatchId",
                table: "BaleMessages");

            migrationBuilder.DropIndex(
                name: "IX_BaleMessages_MessageChannelId",
                table: "BaleMessages");

            migrationBuilder.DropIndex(
                name: "IX_BaleMessages_PersonGroupId",
                table: "BaleMessages");

            migrationBuilder.DropColumn(
                name: "BroadcastBatchId",
                table: "BaleMessages");

            migrationBuilder.DropColumn(
                name: "MessageChannelId",
                table: "BaleMessages");

            migrationBuilder.DropColumn(
                name: "MessengerKind",
                table: "BaleMessages");

            migrationBuilder.DropColumn(
                name: "PersonGroupId",
                table: "BaleMessages");
        }
    }
}

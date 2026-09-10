using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using JameJafari.Infrastructure.Data;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260910220500_RenameMessengerMessages")]
public partial class RenameMessengerMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "BaleMessages",
            newName: "MessengerMessages");

        migrationBuilder.RenameColumn(
            name: "BaleMessageId",
            table: "MessengerMessages",
            newName: "RemoteMessageId");

        migrationBuilder.RenameColumn(
            name: "BaleMessageIds",
            table: "MessengerMessages",
            newName: "RemoteMessageIds");

        migrationBuilder.RenameColumn(
            name: "DeletedFromBale",
            table: "MessengerMessages",
            newName: "DeletedFromRemote");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "DeletedFromRemote",
            table: "MessengerMessages",
            newName: "DeletedFromBale");

        migrationBuilder.RenameColumn(
            name: "RemoteMessageIds",
            table: "MessengerMessages",
            newName: "BaleMessageIds");

        migrationBuilder.RenameColumn(
            name: "RemoteMessageId",
            table: "MessengerMessages",
            newName: "BaleMessageId");

        migrationBuilder.RenameTable(
            name: "MessengerMessages",
            newName: "BaleMessages");
    }
}

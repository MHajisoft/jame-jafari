using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using JameJafari.Infrastructure.Data;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260911050000_AddRubikaKnownGroupChats")]
public partial class AddRubikaKnownGroupChats : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "KnownGroupChatsJson",
            table: "RubikaBotStates",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "KnownGroupChatsJson",
            table: "RubikaBotStates");
    }
}

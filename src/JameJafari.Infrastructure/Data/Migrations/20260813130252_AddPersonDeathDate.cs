using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonDeathDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeathDate",
                table: "Persons",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeathDate",
                table: "Persons");
        }
    }
}

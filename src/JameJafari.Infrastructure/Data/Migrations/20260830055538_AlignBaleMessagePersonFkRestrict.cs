using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignBaleMessagePersonFkRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaleMessages_Persons_PersonId",
                table: "BaleMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_BaleMessages_Persons_PersonId",
                table: "BaleMessages",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaleMessages_Persons_PersonId",
                table: "BaleMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_BaleMessages_Persons_PersonId",
                table: "BaleMessages",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

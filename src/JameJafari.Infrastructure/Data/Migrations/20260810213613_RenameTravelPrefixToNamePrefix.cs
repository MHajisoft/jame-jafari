using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameTravelPrefixToNamePrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Persons_GeneralTypes_TravelPrefixId",
                table: "Persons");

            migrationBuilder.RenameColumn(
                name: "TravelPrefixId",
                table: "Persons",
                newName: "NamePrefixId");

            migrationBuilder.RenameIndex(
                name: "IX_Persons_TravelPrefixId",
                table: "Persons",
                newName: "IX_Persons_NamePrefixId");

            migrationBuilder.AddForeignKey(
                name: "FK_Persons_GeneralTypes_NamePrefixId",
                table: "Persons",
                column: "NamePrefixId",
                principalTable: "GeneralTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Persons_GeneralTypes_NamePrefixId",
                table: "Persons");

            migrationBuilder.RenameColumn(
                name: "NamePrefixId",
                table: "Persons",
                newName: "TravelPrefixId");

            migrationBuilder.RenameIndex(
                name: "IX_Persons_NamePrefixId",
                table: "Persons",
                newName: "IX_Persons_TravelPrefixId");

            migrationBuilder.AddForeignKey(
                name: "FK_Persons_GeneralTypes_TravelPrefixId",
                table: "Persons",
                column: "TravelPrefixId",
                principalTable: "GeneralTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

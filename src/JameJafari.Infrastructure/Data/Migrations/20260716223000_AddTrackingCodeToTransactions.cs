using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260716223000_AddTrackingCodeToTransactions")]
public partial class AddTrackingCodeToTransactions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TrackingCode",
            table: "IncomeTransactions",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TrackingCode",
            table: "CostTransactions",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TrackingCode",
            table: "IncomeTransactions");

        migrationBuilder.DropColumn(
            name: "TrackingCode",
            table: "CostTransactions");
    }
}

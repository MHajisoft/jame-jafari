using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260812120000_AddTransactionAttachments")]
    public partial class AddTransactionAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IncomeTransactionId = table.Column<int>(type: "int", nullable: true),
                    CostTransactionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionAttachments_CostTransactions_CostTransactionId",
                        column: x => x.CostTransactionId,
                        principalTable: "CostTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionAttachments_IncomeTransactions_IncomeTransactionId",
                        column: x => x.IncomeTransactionId,
                        principalTable: "IncomeTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO TransactionAttachments (Path, IncomeTransactionId, CostTransactionId, CreatedAt)
                SELECT DocumentPath, Id, NULL, COALESCE(CreatedAt, GETUTCDATE())
                FROM IncomeTransactions
                WHERE DocumentPath IS NOT NULL AND LTRIM(RTRIM(DocumentPath)) <> '';

                INSERT INTO TransactionAttachments (Path, IncomeTransactionId, CostTransactionId, CreatedAt)
                SELECT DocumentPath, NULL, Id, COALESCE(CreatedAt, GETUTCDATE())
                FROM CostTransactions
                WHERE DocumentPath IS NOT NULL AND LTRIM(RTRIM(DocumentPath)) <> '';
                """);

            migrationBuilder.DropColumn(
                name: "DocumentPath",
                table: "IncomeTransactions");

            migrationBuilder.DropColumn(
                name: "DocumentPath",
                table: "CostTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAttachments_CostTransactionId",
                table: "TransactionAttachments",
                column: "CostTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAttachments_IncomeTransactionId",
                table: "TransactionAttachments",
                column: "IncomeTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentPath",
                table: "IncomeTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentPath",
                table: "CostTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE i SET DocumentPath = x.Path
                FROM IncomeTransactions i
                INNER JOIN (
                    SELECT IncomeTransactionId, MIN(Path) AS Path
                    FROM TransactionAttachments
                    WHERE IncomeTransactionId IS NOT NULL
                    GROUP BY IncomeTransactionId
                ) x ON x.IncomeTransactionId = i.Id;

                UPDATE c SET DocumentPath = x.Path
                FROM CostTransactions c
                INNER JOIN (
                    SELECT CostTransactionId, MIN(Path) AS Path
                    FROM TransactionAttachments
                    WHERE CostTransactionId IS NOT NULL
                    GROUP BY CostTransactionId
                ) x ON x.CostTransactionId = c.Id;
                """);

            migrationBuilder.DropTable(
                name: "TransactionAttachments");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JameJafari.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportIndexesAndQueryFilterSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncomeTransactions_AccountId",
                table: "IncomeTransactions");

            migrationBuilder.DropIndex(
                name: "IX_IncomeTransactions_CostTypeId",
                table: "IncomeTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CostTransactions_AccountId",
                table: "CostTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CostTransactions_CostTypeId",
                table: "CostTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_IncomeTransactions_AccountId_TransactionDate",
                table: "IncomeTransactions",
                columns: new[] { "AccountId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomeTransactions_CostTypeId_TransactionDate",
                table: "IncomeTransactions",
                columns: new[] { "CostTypeId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomeTransactions_IsDeleted_TransactionDate",
                table: "IncomeTransactions",
                columns: new[] { "IsDeleted", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneralTypes_Category_IsDeleted_SortOrder",
                table: "GeneralTypes",
                columns: new[] { "Category", "IsDeleted", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FoodGenerations_CookDate",
                table: "FoodGenerations",
                column: "CookDate");

            migrationBuilder.CreateIndex(
                name: "IX_CostTransactions_AccountId_TransactionDate",
                table: "CostTransactions",
                columns: new[] { "AccountId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CostTransactions_CostTypeId_TransactionDate",
                table: "CostTransactions",
                columns: new[] { "CostTypeId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CostTransactions_IsDeleted_TransactionDate",
                table: "CostTransactions",
                columns: new[] { "IsDeleted", "TransactionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncomeTransactions_AccountId_TransactionDate",
                table: "IncomeTransactions");

            migrationBuilder.DropIndex(
                name: "IX_IncomeTransactions_CostTypeId_TransactionDate",
                table: "IncomeTransactions");

            migrationBuilder.DropIndex(
                name: "IX_IncomeTransactions_IsDeleted_TransactionDate",
                table: "IncomeTransactions");

            migrationBuilder.DropIndex(
                name: "IX_GeneralTypes_Category_IsDeleted_SortOrder",
                table: "GeneralTypes");

            migrationBuilder.DropIndex(
                name: "IX_FoodGenerations_CookDate",
                table: "FoodGenerations");

            migrationBuilder.DropIndex(
                name: "IX_CostTransactions_AccountId_TransactionDate",
                table: "CostTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CostTransactions_CostTypeId_TransactionDate",
                table: "CostTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CostTransactions_IsDeleted_TransactionDate",
                table: "CostTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_IncomeTransactions_AccountId",
                table: "IncomeTransactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomeTransactions_CostTypeId",
                table: "IncomeTransactions",
                column: "CostTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CostTransactions_AccountId",
                table: "CostTransactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CostTransactions_CostTypeId",
                table: "CostTransactions",
                column: "CostTypeId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "BudgetMonths",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetMonths_UserId",
                table: "BudgetMonths",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetMonths_Users_UserId",
                table: "BudgetMonths",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetMonths_Users_UserId",
                table: "BudgetMonths");

            migrationBuilder.DropIndex(
                name: "IX_BudgetMonths_UserId",
                table: "BudgetMonths");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "BudgetMonths");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetTracker.Migrations
{
    /// <inheritdoc />
    public partial class AdoptUnownedBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Budgets created before ownership existed have no UserId. When exactly one
            // account exists, that person is unambiguously the owner, so hand the data
            // over rather than leaving it stranded and invisible. With zero accounts the
            // first person to register adopts it; with several, ownership is ambiguous
            // and the rows are deliberately left alone.
            //
            // This lives in its own migration because SQLite rebuilds BudgetMonths to add
            // the foreign key, and EF warns (ASP0026-style, EF 30200) that data operations
            // queued against a pending table rebuild may see an unexpected state.
            migrationBuilder.Sql(@"
                UPDATE BudgetMonths
                SET UserId = (SELECT Id FROM Users ORDER BY Id LIMIT 1)
                WHERE UserId IS NULL
                  AND (SELECT COUNT(*) FROM Users) = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

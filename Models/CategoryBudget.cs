namespace BudgetTracker.Models;

public class CategoryBudget
{
    public int Id { get; set; }
    public int BudgetMonthId { get; set; }
    public BudgetMonth? BudgetMonth { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }

    public List<Expense> Expenses { get; set; } = new();

    public decimal Spent => Expenses.Sum(e => e.Amount);
    public decimal Remaining => BudgetAmount - Spent;
}
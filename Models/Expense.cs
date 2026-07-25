namespace BudgetTracker.Models;

public class Expense
{
    public int Id { get; set; }
    public int CategoryBudgetId { get; set; }
    public CategoryBudget? CategoryBudget { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.Today;
}
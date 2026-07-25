namespace BudgetTracker.Models;

public class BudgetMonth
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalBudget { get; set; }

    public List<CategoryBudget> Categories { get; set; } = new();
}
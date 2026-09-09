namespace BudgetTracker.Models;

public class BudgetMonth
{
    public int Id { get; set; }

    // Nullable only so the column could be added to a database that already had rows.
    // Budgets created by the app always set it, and every query filters on an exact id,
    // so an unowned row is visible to nobody.
    public int? UserId { get; set; }
    public AppUser? User { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalBudget { get; set; }

    public List<CategoryBudget> Categories { get; set; } = new();
}

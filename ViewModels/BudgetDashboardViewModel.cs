namespace BudgetTracker.ViewModels;

public class BudgetDashboardViewModel
{
    public int? BudgetMonthId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }

    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining => TotalBudget - TotalSpent;

    public List<BudgetMonthSummaryViewModel> AvailableBudgets { get; set; } = new();
    public List<CategoryBudgetViewModel> Categories { get; set; } = new();
}

public class BudgetMonthSummaryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalBudget { get; set; }
    public bool IsSelected { get; set; }
}

public class CategoryBudgetViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining => BudgetAmount - Spent;

    public List<ExpenseViewModel> Expenses { get; set; } = new();
}

public class ExpenseViewModel
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PurchasedAt { get; set; }
}
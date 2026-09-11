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

    public bool HasPreviousMonthBudget { get; set; }

    public int PercentUsed => BudgetProgress.PercentUsed(TotalSpent, TotalBudget);
    public int BarPercent => BudgetProgress.BarPercent(PercentUsed);
    public bool IsOverspent => TotalSpent > TotalBudget;

    public List<BudgetMonthSummaryViewModel> AvailableBudgets { get; set; } = new();
    public List<CategoryBudgetViewModel> Categories { get; set; } = new();

    public decimal TotalCategoryAllocation => Categories.Sum(c => c.BudgetAmount);
    public decimal Unallocated => TotalBudget - TotalCategoryAllocation;
    public bool IsOverAllocated => TotalCategoryAllocation > TotalBudget;
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

    public int PercentUsed => BudgetProgress.PercentUsed(Spent, BudgetAmount);
    public int BarPercent => BudgetProgress.BarPercent(PercentUsed);
    public bool IsOverspent => Spent > BudgetAmount;
    public bool IsNearLimit => !IsOverspent && PercentUsed >= 80;

    public List<ExpenseViewModel> Expenses { get; set; } = new();
}

public class ExpenseViewModel
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PurchasedAt { get; set; }
}

internal static class BudgetProgress
{
    public static int PercentUsed(decimal spent, decimal budget)
    {
        if (budget <= 0)
        {
            return spent > 0 ? 100 : 0;
        }

        return (int)Math.Round(spent / budget * 100, MidpointRounding.AwayFromZero);
    }

    public static int BarPercent(int percentUsed)
    {
        return Math.Clamp(percentUsed, 0, 100);
    }
}

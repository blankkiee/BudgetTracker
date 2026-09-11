using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.ViewModels;

public class CreateBudgetRequest
{
    [Range(2000, 2100, ErrorMessage = "Choose a year between 2000 and 2100.")]
    public int Year { get; set; }

    [Range(1, 12, ErrorMessage = "Choose a valid month before creating a budget.")]
    public int Month { get; set; }

    [Required(ErrorMessage = "Budget name is required.")]
    [StringLength(100, ErrorMessage = "Budget name cannot be longer than 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, MoneyLimits.Max, ErrorMessage = "Budget amount must be between 0 and 1,000,000,000.")]
    public decimal TotalBudget { get; set; }

    public bool CopyCategoriesFromPreviousMonth { get; set; }
}

public class UpdateBudgetRequest
{
    public int BudgetMonthId { get; set; }

    [Required(ErrorMessage = "Budget name is required.")]
    [StringLength(100, ErrorMessage = "Budget name cannot be longer than 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, MoneyLimits.Max, ErrorMessage = "Budget amount must be between 0 and 1,000,000,000.")]
    public decimal TotalBudget { get; set; }
}

public class AddCategoryRequest
{
    public int BudgetMonthId { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, ErrorMessage = "Category name cannot be longer than 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, MoneyLimits.Max, ErrorMessage = "Category budget must be between 0 and 1,000,000,000.")]
    public decimal BudgetAmount { get; set; }
}

public class SaveCategoryRequest
{
    public int CategoryBudgetId { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, ErrorMessage = "Category name cannot be longer than 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, MoneyLimits.Max, ErrorMessage = "Category budget must be between 0 and 1,000,000,000.")]
    public decimal BudgetAmount { get; set; }
}

public class AddExpenseRequest
{
    public int CategoryBudgetId { get; set; }

    [Required(ErrorMessage = "Expense description is required.")]
    [StringLength(200, ErrorMessage = "Expense description cannot be longer than 200 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(0, MoneyLimits.Max, ErrorMessage = "Expense amount must be between 0 and 1,000,000,000.")]
    public decimal Amount { get; set; }

    public DateTime? PurchasedAt { get; set; }
}

public class SaveExpenseRequest
{
    public int ExpenseId { get; set; }

    [Required(ErrorMessage = "Expense description is required.")]
    [StringLength(200, ErrorMessage = "Expense description cannot be longer than 200 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(0, MoneyLimits.Max, ErrorMessage = "Expense amount must be between 0 and 1,000,000,000.")]
    public decimal Amount { get; set; }

    public DateTime? PurchasedAt { get; set; }
}

internal static class MoneyLimits
{
    public const int Max = 1_000_000_000;
}
